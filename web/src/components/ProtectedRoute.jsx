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
      <div className="flex flex-col items-center justify-center min-h-[60vh] text-center p-6 bg-slate-50">
        <div className="bg-white border border-slate-200 p-8 rounded-2xl shadow-xs max-w-md w-full">
          <h2 className="text-xl font-bold text-red-600 mb-2">Access Restricted</h2>
          <p className="text-xs text-slate-500 mb-5 leading-relaxed">
            Your assigned role (<span className="font-semibold text-slate-800">{user?.role || 'Unknown'}</span>) does not have authorization to access this operational module.
          </p>
          <a
            href="/"
            className="inline-flex items-center justify-center px-4 py-2 text-xs font-semibold rounded-xl bg-waypoint-primary text-white hover:bg-waypoint-primary-hover transition-colors shadow-xs"
          >
            Return to Dashboard
          </a>
        </div>
      </div>
    )
  }

  return <Outlet />
}
