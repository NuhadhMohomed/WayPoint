import React, { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { OfflineBanner } from '../components/ui/OfflineBanner'
import { CommandPalette } from '../components/ui/CommandPalette'
import { KeyboardShortcutsModal } from '../components/ui/KeyboardShortcutsModal'
import {
  Compass,
  LayoutDashboard,
  MapPin,
  Calendar,
  Sparkles,
  Bus,
  Grid3X3,
  Users,
  Star,
  FileSpreadsheet,
  AlertTriangle,
  CheckSquare,
  Radio,
  Cpu,
  ShieldAlert,
  LogOut,
  Search,
  Menu,
  X,
  HelpCircle
} from 'lucide-react'

export function DashboardLayout() {
  const { user, logout } = useAuthStore()
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false)
  const [isShortcutsOpen, setIsShortcutsOpen] = useState(false)
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const navGroups = [
    {
      title: 'Operations Overview',
      items: [
        { label: 'System Overview', to: '/', icon: LayoutDashboard },
      ]
    },
    {
      title: 'Routes & Network',
      items: [
        { label: 'Route Catalogue', to: '/routes/catalog', icon: MapPin },
        { label: 'Service Scheduler', to: '/routes/scheduler', icon: Calendar },
        { label: 'Tourist Corridors', to: '/routes/corridors', icon: Sparkles },
      ]
    },
    {
      title: 'Fleet Management',
      items: [
        { label: 'Fleet Matrix', to: '/fleet/buses', icon: Bus },
        { label: 'Seat Layout Designer', to: '/fleet/layouts', icon: Grid3X3 },
        { label: 'Driver Rostering', to: '/fleet/drivers', icon: Users },
        { label: 'Fleet Reviews', to: '/fleet/reviews', icon: Star },
      ]
    },
    {
      title: 'Ticketing & Manifest',
      items: [
        { label: 'Passenger Manifest', to: '/bookings', icon: FileSpreadsheet },
      ]
    },
    {
      title: 'Incident Operations',
      items: [
        { label: 'Disruption Intake', to: '/disruptions/intake', icon: AlertTriangle },
        { label: 'Manager Approvals', to: '/disruptions/approvals', icon: CheckSquare },
        { label: 'Service Alerts', to: '/disruptions/alerts', icon: Radio },
        { label: 'AI Observability', to: '/disruptions/ai-traces', icon: Cpu },
      ]
    },
    {
      title: 'System Administration',
      items: [
        {
          label: 'User Governance',
          to: '/admin/users',
          icon: ShieldAlert,
          badge: user?.role === 'Admin' ? null : 'Admin Only',
        },
      ]
    }
  ]

  const sidebarContent = (
    <div className="flex flex-col h-full bg-white text-slate-700 border-r border-slate-200">
      {/* Brand Header */}
      <div className="p-4 border-b border-slate-200 flex items-center justify-between bg-white">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-lg bg-indigo-600 text-white flex items-center justify-center font-bold shadow-md shadow-indigo-600/30">
            <Compass className="w-5 h-5" />
          </div>
          <div>
            <h1 className="text-sm font-bold text-slate-900 tracking-tight flex items-center gap-1.5">
              WayPoint
              <span className="text-[10px] px-1.5 py-0.2 bg-indigo-50 text-indigo-700 border border-indigo-200 rounded font-mono font-semibold">
                NOC
              </span>
            </h1>
            <p className="text-[10px] text-slate-500">Transit Operations Hub</p>
          </div>
        </div>
      </div>

      {/* Nav Items */}
      <div className="flex-1 overflow-y-auto p-3 space-y-5 text-xs">
        {navGroups.map((group, gIdx) => (
          <div key={gIdx} className="space-y-1">
            <div className="px-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">
              {group.title}
            </div>
            {group.items.map((item, iIdx) => {
              const Icon = item.icon
              return (
                <NavLink
                  key={iIdx}
                  to={item.to}
                  end={item.to === '/'}
                  onClick={() => setMobileMenuOpen(false)}
                  className={({ isActive }) =>
                    `flex items-center gap-2.5 px-2.5 py-2 rounded-lg font-medium transition-all ${
                      isActive
                        ? 'bg-indigo-600 text-white shadow-sm shadow-indigo-600/25 font-semibold'
                        : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                    }`
                  }
                >
                  <Icon className="w-4 h-4 flex-shrink-0" />
                  <span className="truncate">{item.label}</span>
                  {item.badge && (
                    <span className="ml-auto text-[9px] px-1.5 py-0.5 rounded font-mono font-bold bg-slate-100 text-slate-500 border border-slate-200">
                      {item.badge}
                    </span>
                  )}
                </NavLink>
              )
            })}
          </div>
        ))}
      </div>

      {/* Footer Profile & Logout */}
      <div className="p-3 border-t border-slate-200 bg-slate-50/80">
        <div className="flex items-center justify-between">
          <div className="truncate">
            <div className="text-xs font-semibold text-slate-900 truncate">{user?.fullName || 'Operator'}</div>
            <div className="text-[10px] text-slate-500 font-mono truncate">{user?.email || 'Active Session'}</div>
          </div>
          <button
            type="button"
            onClick={handleLogout}
            className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
            title="Log out"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  )

  return (
    <div className="flex flex-col h-screen bg-slate-50 text-slate-800 antialiased overflow-hidden">
      <OfflineBanner />

      <div className="flex flex-1 overflow-hidden">
        {/* Desktop Sidebar */}
        <aside className="hidden md:flex w-64 flex-shrink-0">
          {sidebarContent}
        </aside>

        {/* Mobile Drawer */}
        {mobileMenuOpen && (
          <div
            className="fixed inset-0 z-50 md:hidden bg-slate-900/40 backdrop-blur-xs"
            onClick={() => setMobileMenuOpen(false)}
          >
            <div
              className="fixed inset-y-0 left-0 w-72 bg-white shadow-2xl flex flex-col justify-between"
              onClick={(e) => e.stopPropagation()}
            >
              {sidebarContent}
            </div>
          </div>
        )}

        {/* Main Content Area */}
        <div className="flex-1 flex flex-col overflow-hidden bg-slate-50">
          {/* Top Header */}
          <header className="h-14 bg-white/95 backdrop-blur-sm border-b border-slate-200 px-4 sm:px-6 flex items-center justify-between gap-4 z-10 shadow-xs">
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => setMobileMenuOpen(true)}
                className="md:hidden p-1.5 rounded-lg text-slate-600 hover:bg-slate-100 border border-slate-200"
              >
                <Menu className="w-5 h-5" />
              </button>

              <button
                type="button"
                onClick={() => setIsCommandPaletteOpen(true)}
                className="flex items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs text-slate-500 hover:border-slate-300 hover:text-slate-800 hover:bg-slate-100 transition-all shadow-xs w-44 sm:w-64"
              >
                <Search className="w-3.5 h-3.5 text-slate-400" />
                <span className="truncate text-left flex-1">Jump to corridor (Ctrl+K)...</span>
                <kbd className="hidden sm:inline-block rounded bg-white px-1 font-mono text-[10px] text-slate-500 border border-slate-200">
                  Ctrl+K
                </kbd>
              </button>
            </div>

            <div className="flex items-center gap-2 sm:gap-3">
              <button
                type="button"
                onClick={() => setIsShortcutsOpen(true)}
                className="p-1.5 rounded-lg border border-slate-200 bg-slate-50 text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition-colors"
                title="Keyboard Shortcuts"
              >
                <HelpCircle className="w-4 h-4" />
              </button>
            </div>
          </header>

          {/* Main Viewport */}
          <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8 bg-slate-50 text-slate-800">
            <Outlet />
          </main>
        </div>
      </div>

      <CommandPalette
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
      />

      <KeyboardShortcutsModal
        isOpen={isShortcutsOpen}
        onClose={() => setIsShortcutsOpen(false)}
      />
    </div>
  )
}
export default DashboardLayout
