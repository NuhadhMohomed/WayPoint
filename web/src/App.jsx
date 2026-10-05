import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { OverviewPage } from './pages/OverviewPage'
import { DashboardLayout } from './layouts/DashboardLayout'
import { ProtectedRoute } from './components/ProtectedRoute'

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Protected Routes inside DashboardLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
          <Route path="/" element={<OverviewPage />} />
          {/* Component 1: Routes & Network */}
          <Route path="/routes/catalog" element={<div className="p-6">Route Catalogue</div>} />
          <Route path="/routes/scheduler" element={<div className="p-6">Service Scheduler</div>} />
          <Route path="/routes/corridors" element={<div className="p-6">Tourist Corridors</div>} />

          {/* Component 2: Fleet & Feasibility */}
          <Route path="/fleet/buses" element={<div className="p-6">Fleet Matrix</div>} />
          <Route path="/fleet/layouts" element={<div className="p-6">Seat Layout Designer</div>} />
          <Route path="/fleet/drivers" element={<div className="p-6">Driver Rostering</div>} />
          <Route path="/fleet/reviews" element={<div className="p-6">Fleet Reviews</div>} />

          {/* Component 3: Manifest & Ticketing */}
          <Route path="/bookings" element={<div className="p-6">Passenger Manifest</div>} />

          {/* Component 4: Disruption & AI Ops */}
          <Route path="/disruptions/intake" element={<div className="p-6">Disruption Intake</div>} />
          <Route path="/disruptions/approvals" element={<div className="p-6">Manager Approvals</div>} />
          <Route path="/disruptions/alerts" element={<div className="p-6">Service Alerts</div>} />
          <Route path="/disruptions/ai-traces" element={<div className="p-6">AI Observability</div>} />

          {/* Governance */}
          <Route path="/admin/users" element={<div className="p-6">User Governance</div>} />
        </Route>
      </Route>

      <Route path="/404" element={<NotFoundPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
