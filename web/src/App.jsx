import React from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { DashboardLayout } from './layouts/DashboardLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { OverviewPage } from './pages/OverviewPage'
import { RoutesPlaceholderPage } from './pages/RoutesPlaceholderPage'
import { BookingsPlaceholderPage } from './pages/BookingsPlaceholderPage'
import { DisruptionsPlaceholderPage } from './pages/DisruptionsPlaceholderPage'
import { FleetPlaceholderPage } from './pages/FleetPlaceholderPage'
import { BookingManifestMonitorPage } from './features/bookings/BookingManifestMonitorPage'
import { OperatorDashboardPage } from './features/bookings/OperatorDashboardPage'
import { DisruptionsPlaceholderPage } from './pages/DisruptionsPlaceholderPage'

>>>>>>> 7503deb75fa02bb488804a82f2bebcfcc21d935c
// Component 2: Fleet, Seat & Resource Feasibility (Nuhadh)
import { FleetHubLayout } from './features/fleet/FleetHubLayout'
import { FleetMatrixBuilderPage } from './features/fleet/FleetMatrixBuilderPage'
import { SeatLayoutDesignerPage } from './features/fleet/SeatLayoutDesignerPage'
import { DriverRosteringPage } from './features/fleet/DriverRosteringPage'
import { BookingManifestMonitorPage } from './features/fleet/BookingManifestMonitorPage'

// Component 4: Disruption, Rebooking & Approval (Dineth)
import { DisruptionHubLayout } from './features/disruptions/DisruptionHubLayout'
import { DisruptionIntakePage } from './features/disruptions/DisruptionIntakePage'
import { ManagerApprovalWorkbenchPage } from './features/disruptions/ManagerApprovalWorkbenchPage'
import { ServiceAlertBroadcastPage } from './features/disruptions/ServiceAlertBroadcastPage'
import { AiObservabilityPage } from './features/disruptions/AiObservabilityPage'
import { AdminConsolePage } from './features/disruptions/AdminConsolePage'

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
          
          {/* Component 1: Sethum */}
          <Route path="/routes" element={<RoutesPlaceholderPage />} />

          {/* Component 2: Nuhadh — Fleet Hub with Tabbed Navigation */}
          <Route path="/fleet" element={<FleetHubLayout />}>
            <Route index element={<Navigate to="buses" replace />} />
            <Route path="buses" element={<FleetMatrixBuilderPage />} />
            <Route path="layouts" element={<SeatLayoutDesignerPage />} />
            <Route path="drivers" element={<DriverRosteringPage />} />
          </Route>
          <Route path="/manifest" element={<BookingManifestMonitorPage />} />

          {/* Component 3: Mithila */}
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
        </Route>
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
