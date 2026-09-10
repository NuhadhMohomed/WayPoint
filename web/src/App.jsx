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

// Component 2: Fleet, Seat & Resource Feasibility (Nuhadh)
import { FleetHubLayout } from './features/fleet/FleetHubLayout'
import { FleetMatrixBuilderPage } from './features/fleet/FleetMatrixBuilderPage'
import { SeatLayoutDesignerPage } from './features/fleet/SeatLayoutDesignerPage'
import { DriverRosteringPage } from './features/fleet/DriverRosteringPage'
import { BookingManifestMonitorPage } from './features/fleet/BookingManifestMonitorPage'

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
          <Route path="/bookings" element={<BookingsPlaceholderPage />} />

          {/* Component 4: Dineth */}
          <Route path="/disruptions" element={<DisruptionsPlaceholderPage />} />
        </Route>
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
