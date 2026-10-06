import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { OverviewPage } from './pages/OverviewPage'
import { DashboardLayout } from './layouts/DashboardLayout'
import { ProtectedRoute } from './components/ProtectedRoute'

// Routes & Network
import { JourneyHubLayout } from './features/journey/JourneyHubLayout'
import { RouteManagerPage } from './features/journey/RouteManagerPage'
import { ServiceSchedulerPage } from './features/journey/ServiceSchedulerPage'
import { TouristCorridorsPage } from './features/journey/TouristCorridorsPage'

// Fleet Management
import { FleetHubLayout } from './features/fleet/FleetHubLayout'
import { FleetMatrixBuilderPage } from './features/fleet/FleetMatrixBuilderPage'
import { SeatLayoutDesignerPage } from './features/fleet/SeatLayoutDesignerPage'
import { DriverRosteringPage } from './features/fleet/DriverRosteringPage'
import { FleetReviewsDashboardPage } from './pages/fleet/FleetReviewsDashboardPage'

// Manifest & Ticketing
import { BookingManifestMonitorPage } from './features/bookings/BookingManifestMonitorPage'
import { OperatorDashboardPage } from './features/bookings/OperatorDashboardPage'

// Disruption & Incident Operations
import { DisruptionHubLayout } from './features/disruptions/DisruptionHubLayout'
import { DisruptionIntakePage } from './features/disruptions/DisruptionIntakePage'
import { ManagerApprovalWorkbenchPage } from './features/disruptions/ManagerApprovalWorkbenchPage'
import { ServiceAlertBroadcastPage } from './features/disruptions/ServiceAlertBroadcastPage'
import { AiObservabilityPage } from './features/disruptions/AiObservabilityPage'
import { AdminConsolePage } from './features/disruptions/AdminConsolePage'

// System Administration
import { AdminHubLayout } from './features/admin/AdminHubLayout'
import { AdminUsersPage } from './features/admin/AdminUsersPage'

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<Navigate to="/login" replace />} />

      {/* Protected Routes inside DashboardLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
          <Route path="/" element={<OverviewPage />} />

          {/* Routes & Network */}
          <Route path="/routes" element={<JourneyHubLayout />}>
            <Route index element={<Navigate to="/routes/catalog" replace />} />
            <Route path="catalog" element={<RouteManagerPage />} />
            <Route path="scheduler" element={<ServiceSchedulerPage />} />
            <Route path="corridors" element={<TouristCorridorsPage />} />
          </Route>

          {/* Fleet Management */}
          <Route path="/fleet" element={<FleetHubLayout />}>
            <Route index element={<Navigate to="/fleet/buses" replace />} />
            <Route path="buses" element={<FleetMatrixBuilderPage />} />
            <Route path="layouts" element={<SeatLayoutDesignerPage />} />
            <Route path="drivers" element={<DriverRosteringPage />} />
            <Route path="reviews" element={<FleetReviewsDashboardPage />} />
          </Route>

          {/* Manifest & Ticketing */}
          <Route path="/bookings" element={<BookingManifestMonitorPage />} />
          <Route path="/bookings/manifest" element={<BookingManifestMonitorPage />} />
          <Route path="/bookings/operator" element={<OperatorDashboardPage />} />

          {/* Disruption & Incident Operations */}
          <Route path="/disruptions" element={<DisruptionHubLayout />}>
            <Route index element={<Navigate to="/disruptions/intake" replace />} />
            <Route path="intake" element={<DisruptionIntakePage />} />
            <Route path="approvals" element={<ManagerApprovalWorkbenchPage />} />
            <Route path="alerts" element={<ServiceAlertBroadcastPage />} />
            <Route path="ai-traces" element={<AiObservabilityPage />} />
            <Route path="admin" element={<AdminConsolePage />} />
          </Route>

          {/* System Administration */}
          <Route path="/admin" element={<AdminHubLayout />}>
            <Route index element={<Navigate to="/admin/users" replace />} />
            <Route path="users" element={<AdminUsersPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="/404" element={<NotFoundPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
