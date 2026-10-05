import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { OverviewPage } from './pages/OverviewPage'
import { DashboardLayout } from './layouts/DashboardLayout'
import { ProtectedRoute } from './components/ProtectedRoute'

// Component 1: Routes & Network (Sethum)
import { JourneyHubLayout } from './features/journey/JourneyHubLayout'
import { RouteManagerPage } from './features/journey/RouteManagerPage'
import { ServiceSchedulerPage } from './features/journey/ServiceSchedulerPage'
import { TouristCorridorsPage } from './features/journey/TouristCorridorsPage'

// Component 2: Fleet & Feasibility (Nuhadh)
import { FleetHubLayout } from './features/fleet/FleetHubLayout'
import { FleetMatrixBuilderPage } from './features/fleet/FleetMatrixBuilderPage'
import { SeatLayoutDesignerPage } from './features/fleet/SeatLayoutDesignerPage'
import { DriverRosteringPage } from './features/fleet/DriverRosteringPage'
import { FleetReviewsDashboardPage } from './pages/fleet/FleetReviewsDashboardPage'

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Protected Routes inside DashboardLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
          <Route path="/" element={<OverviewPage />} />

          {/* Component 1: Routes & Network (Sethum) */}
          <Route path="/routes" element={<JourneyHubLayout />}>
            <Route index element={<Navigate to="/routes/catalog" replace />} />
            <Route path="catalog" element={<RouteManagerPage />} />
            <Route path="scheduler" element={<ServiceSchedulerPage />} />
            <Route path="corridors" element={<TouristCorridorsPage />} />
          </Route>

          {/* Component 2: Fleet & Feasibility (Nuhadh) */}
          <Route path="/fleet" element={<FleetHubLayout />}>
            <Route index element={<Navigate to="/fleet/buses" replace />} />
            <Route path="buses" element={<FleetMatrixBuilderPage />} />
            <Route path="layouts" element={<SeatLayoutDesignerPage />} />
            <Route path="drivers" element={<DriverRosteringPage />} />
            <Route path="reviews" element={<FleetReviewsDashboardPage />} />
          </Route>

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
