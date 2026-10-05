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

// Component 3: Manifest & Ticketing (Mithila)
import { BookingManifestMonitorPage } from './features/bookings/BookingManifestMonitorPage'
import { OperatorDashboardPage } from './features/bookings/OperatorDashboardPage'

// Component 4: Disruption & AI Ops (Dineth)
import { DisruptionHubLayout } from './features/disruptions/DisruptionHubLayout'
import { DisruptionIntakePage } from './features/disruptions/DisruptionIntakePage'
import { ManagerApprovalWorkbenchPage } from './features/disruptions/ManagerApprovalWorkbenchPage'
import { ServiceAlertBroadcastPage } from './features/disruptions/ServiceAlertBroadcastPage'
import { AiObservabilityPage } from './features/disruptions/AiObservabilityPage'
import { AdminConsolePage } from './features/disruptions/AdminConsolePage'

// Platform Governance (Admin)
import { AdminHubLayout } from './features/admin/AdminHubLayout'
import { AdminUsersPage } from './features/admin/AdminUsersPage'

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

          {/* Component 3: Manifest & Ticketing (Mithila) */}
          <Route path="/bookings" element={<BookingManifestMonitorPage />} />
          <Route path="/bookings/manifest" element={<BookingManifestMonitorPage />} />
          <Route path="/bookings/operator" element={<OperatorDashboardPage />} />

          {/* Component 4: Disruption & AI Ops (Dineth) */}
          <Route path="/disruptions" element={<DisruptionHubLayout />}>
            <Route index element={<Navigate to="/disruptions/intake" replace />} />
            <Route path="intake" element={<DisruptionIntakePage />} />
            <Route path="approvals" element={<ManagerApprovalWorkbenchPage />} />
            <Route path="alerts" element={<ServiceAlertBroadcastPage />} />
            <Route path="ai-traces" element={<AiObservabilityPage />} />
            <Route path="admin" element={<AdminConsolePage />} />
          </Route>

          {/* Platform Governance */}
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
