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
      <div className="flex flex-col items-center justify-center min-h-[60vh] text-center p-6">
        <h2 className="text-2xl font-bold text-destructive mb-2">Access Denied</h2>
        <p className="text-muted-foreground mb-4">
          Your role (<span className="font-semibold">{user?.role || 'Unknown'}</span>) does not have permission to view this section.
        </p>
        <a href="/" className="text-primary hover:underline text-sm font-medium">
          Return to Dashboard
        </a>
      </div>
    )
  }

  return <Outlet />
}
