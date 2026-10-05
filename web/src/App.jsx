import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { DashboardLayout } from './layouts/DashboardLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { OverviewPage } from './pages/OverviewPage'
import { NotFoundPage } from './pages/NotFoundPage'

// Component 3: Booking & Operator (Mithila)
import { OperatorDashboardPage } from './features/bookings/OperatorDashboardPage'
import { BookingManifestMonitorPage } from './features/bookings/BookingManifestMonitorPage'

// Component 1: Journey Planning & Route Catalogue (Sethum)
import { JourneyHubLayout } from './features/journey/JourneyHubLayout'
import { RouteManagerPage } from './features/journey/RouteManagerPage'
import { ServiceSchedulerPage } from './features/journey/ServiceSchedulerPage'
import { TouristCorridorsPage } from './features/journey/TouristCorridorsPage'

// Component 2: Fleet, Seat & Resource Feasibility (Nuhadh)
import { FleetHubLayout } from './features/fleet/FleetHubLayout'
import { FleetMatrixBuilderPage } from './features/fleet/FleetMatrixBuilderPage'
import { SeatLayoutDesignerPage } from './features/fleet/SeatLayoutDesignerPage'
import { DriverRosteringPage } from './features/fleet/DriverRosteringPage'

// Component 2: Fleet Reviews (Nuhadh — SCR-FLEET-101)
import FleetReviewsDashboardPage from './pages/fleet/FleetReviewsDashboardPage'

// Component 4: Disruption, Approval, Alerts & AI (Dineth)
import { DisruptionHubLayout } from './features/disruptions/DisruptionHubLayout'
import { DisruptionIntakePage } from './features/disruptions/DisruptionIntakePage'
import { ManagerApprovalWorkbenchPage } from './features/disruptions/ManagerApprovalWorkbenchPage'
import { ServiceAlertBroadcastPage } from './features/disruptions/ServiceAlertBroadcastPage'
import { AiObservabilityPage } from './features/disruptions/AiObservabilityPage'
import { AdminConsolePage } from './features/disruptions/AdminConsolePage'

// Administration & User Governance Hub
import { AdminHubLayout } from './features/admin/AdminHubLayout'
import { AdminUsersPage } from './features/admin/AdminUsersPage'

export function App() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Protected Routes inside DashboardLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
          <Route path="/" element={<OverviewPage />} />
          
          {/* Component 1: Sethum — Journey Hub with Tabbed Navigation */}
          <Route path="/routes" element={<JourneyHubLayout />}>
            <Route index element={<Navigate to="catalog" replace />} />
            <Route path="catalog" element={<RouteManagerPage />} />
            <Route path="scheduler" element={<ServiceSchedulerPage />} />
            <Route path="corridors" element={<TouristCorridorsPage />} />
          </Route>

          {/* Component 2: Nuhadh — Fleet Hub with Tabbed Navigation */}
          <Route path="/fleet" element={<FleetHubLayout />}>
            <Route index element={<Navigate to="buses" replace />} />
            <Route path="buses" element={<FleetMatrixBuilderPage />} />
            <Route path="layouts" element={<SeatLayoutDesignerPage />} />
            <Route path="drivers" element={<DriverRosteringPage />} />
          </Route>

          {/* Component 2: Nuhadh — Fleet Reviews Dashboard (SCR-FLEET-101) */}
          <Route path="/fleet/reviews" element={<FleetReviewsDashboardPage />} />
          <Route path="/fleet/reviews/:entityType/:entityId" element={<FleetReviewsDashboardPage />} />

          {/* Component 3: Mithila — Booking & Operator */}
          <Route path="/manifest" element={<BookingManifestMonitorPage />} />
          <Route path="/operator" element={<OperatorDashboardPage />} />
          <Route path="/bookings" element={<BookingManifestMonitorPage />} />

          {/* Component 4: Dineth — Disruption Hub with Tabbed Navigation */}
          <Route path="/disruptions" element={<DisruptionHubLayout />}>
            <Route index element={<Navigate to="intake" replace />} />
            <Route path="intake" element={<DisruptionIntakePage />} />
            <Route path="approvals" element={<ManagerApprovalWorkbenchPage />} />
            <Route path="alerts" element={<ServiceAlertBroadcastPage />} />
            <Route path="ai-traces" element={<AiObservabilityPage />} />
            <Route path="admin" element={<AdminConsolePage />} />
          </Route>

          {/* Administration & User Governance Hub */}
          <Route path="/admin" element={<AdminHubLayout />}>
            <Route index element={<Navigate to="users" replace />} />
            <Route path="users" element={<AdminUsersPage />} />
            <Route path="audit" element={<AdminConsolePage />} />
          </Route>
        </Route>
      </Route>

      {/* Fallback 404 */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
