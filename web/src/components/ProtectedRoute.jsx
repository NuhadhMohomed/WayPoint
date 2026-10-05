import React from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

export function ProtectedRoute({ allowedRoles }) {
  const { isAuthenticated, user } = useAuthStore()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (allowedRoles && (!user?.role || !allowedRoles.includes(user.role))) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] text-center p-6 bg-slate-900 text-slate-100">
        <h2 className="text-2xl font-bold text-red-500 mb-2">Access Denied</h2>
        <p className="text-slate-400 mb-4">
          Your role (<span className="font-semibold text-slate-200">{user?.role || 'Unknown'}</span>) does not have permission to view this section.
        </p>
        <a href="/" className="text-indigo-400 hover:underline text-sm font-medium">
          Return to Dashboard
        </a>
      </div>
    )
  }

  return <Outlet />
}
