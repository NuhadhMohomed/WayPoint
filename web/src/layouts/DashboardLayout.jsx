import React, { useState, useEffect } from 'react'
import { NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { useThemeStore } from '../store/themeStore'
import { devApi } from '../api/client'
import { 
  CommandPalette, 
  KeyboardShortcutsModal, 
  OfflineBanner, 
  Breadcrumbs 
} from '../components/ui'
import { 
  Compass, 
  MapPin, 
  Bus, 
  Ticket, 
  AlertTriangle, 
  LayoutDashboard, 
  LogOut, 
  Database,
  RefreshCw,
  CheckCircle2,
  AlertCircle,
  Menu,
  X,
  Search,
  Sun,
  Moon,
  Clock,
  Sliders,
  HelpCircle,
  Layers,
  ShieldCheck
} from 'lucide-react'

export function DashboardLayout() {
  const { user, logout } = useAuthStore()
  const navigate = useNavigate()
  const location = useLocation()
  const { theme, density, toggleTheme, toggleDensity } = useThemeStore()

  // Modals & Drawers state
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false)
  const [isShortcutsOpen, setIsShortcutsOpen] = useState(false)

  // Seed DB state
  const [seeding, setSeeding] = useState(false)
  const [seedMessage, setSeedMessage] = useState(null)

  // SLST Real-time Clock (Asia/Colombo, UTC+05:30)
  const [slstTime, setSlstTime] = useState('')

  useEffect(() => {
    const updateTime = () => {
      try {
        const formatter = new Intl.DateTimeFormat('en-GB', {
          timeZone: 'Asia/Colombo',
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
          hour12: false,
        })
        setSlstTime(formatter.format(new Date()))
      } catch {
        const d = new Date()
        setSlstTime(d.toTimeString().split(' ')[0])
      }
    }

    updateTime()
    const timer = setInterval(updateTime, 1000)
    return () => clearInterval(timer)
  }, [])

  // Close mobile drawer on route change
  useEffect(() => {
    setMobileMenuOpen(false)
  }, [location.pathname])

  // Global hotkeys listener (Ctrl+K, ?, Alt+T, Alt+D)
  useEffect(() => {
    const handleKeyDown = (e) => {
      // Don't trigger if user is typing in input or textarea (except Escape or Ctrl+K)
      const targetTag = e.target.tagName?.toLowerCase()
      const isInput = targetTag === 'input' || targetTag === 'textarea' || targetTag === 'select'

      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setIsCommandPaletteOpen((prev) => !prev)
        return
      }

      if (e.altKey && e.key.toLowerCase() === 't') {
        e.preventDefault()
        toggleTheme()
        return
      }

      if (e.altKey && e.key.toLowerCase() === 'd') {
        e.preventDefault()
        toggleDensity()
        return
      }

      if (!isInput && e.key === '?') {
        e.preventDefault()
        setIsShortcutsOpen(true)
        return
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [toggleTheme, toggleDensity])

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const handleSeedDatabase = async () => {
    try {
      setSeeding(true)
      setSeedMessage(null)
      await devApi.seedDatabase()
      setSeedMessage({ type: 'success', text: 'Database seeded with Sri Lankan corridors!' })
      setTimeout(() => setSeedMessage(null), 5000)
    } catch (err) {
      setSeedMessage({ type: 'error', text: err.response?.data?.detail || 'Failed to seed database' })
      setTimeout(() => setSeedMessage(null), 5000)
    } finally {
      setSeeding(false)
    }
  }

  const navItems = [
    {
      to: '/',
      label: 'Overview Cockpit',
      icon: LayoutDashboard,
      roles: ['Admin', 'TransportManager', 'Operator', 'Passenger']
    },
    {
      to: '/routes',
      label: 'Routes & Timetables',
      subtext: 'Catalog, Service Scheduling & Corridors',
      icon: MapPin,
      roles: ['Admin', 'TransportManager', 'Operator']
    },
    {
      to: '/fleet',
      label: 'Fleet Studio & Rostering',
      subtext: 'Bus Matrix, Seat Layouts & HOS Roster',
      icon: Bus,
      roles: ['Admin', 'TransportManager', 'Operator']
    },
    {
      to: '/fleet/reviews',
      label: 'Fleet & Service Reviews',
      subtext: 'SCR-FLEET-101 Verified Sentiment',
      icon: Layers,
      roles: ['Admin', 'TransportManager', 'Operator', 'Passenger']
    },
    {
      to: '/operator',
      label: 'Operator NOC Desk',
      subtext: 'Live Dispatch Operations',
      icon: Compass,
      roles: ['Admin', 'TransportManager', 'Operator', 'Passenger']
    },
    {
      to: '/bookings',
      label: 'Manifest & Ticketing',
      subtext: 'Passenger Manifest, Check-In & Refunds',
      icon: Ticket,
      roles: ['Admin', 'TransportManager', 'Operator', 'Passenger']
    },
    {
      to: '/disruptions',
      label: 'Disruption & AI Desk',
      subtext: 'Incidents, Approvals, Alerts & Traces',
      icon: AlertTriangle,
      roles: ['Admin', 'TransportManager', 'Operator']
    },
    {
      to: '/admin',
      label: 'Admin Console & Users',
      subtext: 'User Directory, Role RBAC & Audit Vault',
      icon: ShieldCheck,
      roles: ['Admin']
    },
  ]

  const userRole = user?.role || 'Passenger'
  const filteredNav = navItems.filter((item) => item.roles.includes(userRole))

  // Compute breadcrumbs from path
  const pathSegments = location.pathname.split('/').filter(Boolean)
  const breadcrumbItems = pathSegments.map((segment, idx) => {
    const url = `/${pathSegments.slice(0, idx + 1).join('/')}`
    const label = segment
      .replace(/-/g, ' ')
      .replace(/\b\w/g, (c) => c.toUpperCase())
    return { label, href: url }
  })

  const sidebarContent = (
    <div className="flex h-full flex-col justify-between">
      <div>
        {/* Brand / Logo */}
        <div className="h-16 flex items-center px-6 border-b border-slate-800 gap-3 bg-slate-950">
          <div className="w-9 h-9 rounded-xl bg-waypoint-primary flex items-center justify-center text-waypoint-onPrimary shadow-lg shadow-waypoint-primary/20">
            <Compass className="w-5 h-5 font-bold" />
          </div>
          <div className="flex-1">
            <div className="flex items-center gap-1.5">
              <span className="font-bold text-lg tracking-tight text-slate-100 font-display">
                WayPoint
              </span>
              <span className="px-1.5 py-0.2 rounded text-[10px] font-bold bg-waypoint-primary/20 text-waypoint-primary border border-waypoint-primary/30">
                NOC
              </span>
            </div>
            <span className="block text-[10px] text-slate-400 font-medium tracking-wider uppercase">
              Sri Lanka Transit Ops
            </span>
          </div>
        </div>

        {/* Navigation Links */}
        <nav className="p-3 space-y-1 overflow-y-auto max-h-[calc(100vh-14rem)]">
          {filteredNav.map((item) => {
            const Icon = item.icon
            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2.5 rounded-xl text-xs font-medium transition-all ${
                    isActive
                      ? 'bg-waypoint-primary text-waypoint-onPrimary font-semibold shadow-md shadow-waypoint-primary/10'
                      : 'text-slate-300 hover:text-white hover:bg-slate-850 hover:bg-slate-800/60'
                  }`
                }
              >
                {({ isActive }) => (
                  <>
                    <Icon className={`w-4 h-4 flex-shrink-0 ${isActive ? 'text-waypoint-onPrimary' : 'text-slate-400'}`} />
                    <div className="flex-1 truncate">
                      <div className="truncate font-medium">{item.label}</div>
                      {item.subtext && (
                        <div
                          className={`text-[10px] truncate ${
                            isActive ? 'text-waypoint-onPrimary/80 font-normal' : 'text-slate-500'
                          }`}
                        >
                          {item.subtext}
                        </div>
                      )}
                    </div>
                  </>
                )}
              </NavLink>
            )
          })}
        </nav>
      </div>

      {/* User Card & Logout */}
      <div className="p-3 border-t border-slate-800 bg-slate-950/80">
        <div className="flex items-center justify-between gap-3 p-2 rounded-xl bg-slate-900 border border-slate-800/80">
          <div className="flex items-center gap-2.5 truncate">
            <div className="w-8 h-8 rounded-lg bg-waypoint-primary/20 border border-waypoint-primary/40 flex items-center justify-center text-waypoint-primary font-bold text-xs uppercase shadow-sm">
              {user?.fullName ? user.fullName[0] : 'U'}
            </div>
            <div className="truncate">
              <div className="text-xs font-semibold text-slate-200 truncate">
                {user?.fullName || 'Transit Officer'}
              </div>
              <span className="inline-block px-1.5 py-0.5 text-[9px] font-bold rounded bg-waypoint-primary/10 text-waypoint-primary border border-waypoint-primary/30">
                {userRole}
              </span>
            </div>
          </div>
          <button
            onClick={handleLogout}
            title="Log out (Sign out of NOC session)"
            className="p-1.5 rounded-lg text-slate-400 hover:text-red-400 hover:bg-slate-800 transition-colors"
            aria-label="Log out"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  )

  return (
    <div className="flex flex-col h-screen bg-slate-950 text-slate-100 antialiased overflow-hidden">
      {/* 1. Offline Banner */}
      <OfflineBanner />

      <div className="flex flex-1 overflow-hidden">
        {/* 2. Desktop Sidebar */}
        <aside className="hidden md:flex w-64 flex-shrink-0 bg-slate-950 border-r border-slate-800 flex-col justify-between">
          {sidebarContent}
        </aside>

        {/* 3. Mobile Slide-Over Drawer */}
        {mobileMenuOpen && (
          <div
            className="fixed inset-0 z-50 md:hidden bg-slate-950/80 backdrop-blur-sm transition-opacity"
            onClick={() => setMobileMenuOpen(false)}
          >
            <div
              className="fixed inset-y-0 left-0 w-72 bg-slate-950 border-r border-slate-800 shadow-2xl flex flex-col justify-between animate-in slide-in-from-left duration-200"
              onClick={(e) => e.stopPropagation()}
            >
              <div className="absolute top-4 right-4">
                <button
                  type="button"
                  onClick={() => setMobileMenuOpen(false)}
                  className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800"
                  aria-label="Close navigation drawer"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>
              {sidebarContent}
            </div>
          </div>
        )}

        {/* 4. Main Content Area */}
        <div className="flex-1 flex flex-col overflow-hidden">
          {/* Top Operations Header */}
          <header className="h-16 bg-slate-950/90 backdrop-blur border-b border-slate-800 px-4 sm:px-6 flex items-center justify-between gap-4 z-10">
            {/* Left: Mobile hamburger & Search bar */}
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => setMobileMenuOpen(true)}
                className="md:hidden p-2 rounded-lg text-slate-300 hover:text-white hover:bg-slate-800 border border-slate-700/60"
                aria-label="Open navigation menu"
              >
                <Menu className="w-5 h-5" />
              </button>

              {/* Command Palette Trigger */}
              <button
                type="button"
                onClick={() => setIsCommandPaletteOpen(true)}
                className="flex items-center gap-2.5 rounded-xl border border-slate-800 bg-slate-900/90 px-3 py-1.5 text-xs text-slate-400 hover:border-slate-700 hover:text-slate-200 transition-all shadow-sm w-44 sm:w-64"
                title="Global Command Palette (Ctrl+K)"
              >
                <Search className="w-3.5 h-3.5 text-slate-500" />
                <span className="truncate text-left flex-1">Jump to corridor or hub...</span>
                <kbd className="hidden sm:inline-block rounded bg-slate-800 px-1.5 py-0.5 font-mono text-[10px] text-slate-400 border border-slate-700">
                  Ctrl+K
                </kbd>
              </button>
            </div>

            {/* Right: SLST Clock, Seed Button & Controls */}
            <div className="flex items-center gap-2 sm:gap-3">
              {/* SLST Clock Pill */}
              <div
                className="hidden lg:flex items-center gap-2 px-3 py-1.5 rounded-xl bg-slate-900 border border-slate-800 text-xs font-mono text-slate-300 shadow-inner"
                title="Sri Lanka Standard Time (Asia/Colombo UTC+05:30)"
              >
                <Clock className="w-3.5 h-3.5 text-waypoint-primary" />
                <span className="font-semibold text-slate-100">{slstTime || '00:00:00'}</span>
                <span className="text-[10px] text-slate-500 font-sans uppercase font-bold">SLST</span>
              </div>

              {/* Seed Database Toast / Message */}
              {seedMessage && (
                <div
                  className={`hidden sm:flex text-xs px-3 py-1 rounded-lg border items-center gap-1.5 ${
                    seedMessage.type === 'success'
                      ? 'bg-emerald-950/60 border-emerald-800 text-emerald-300'
                      : 'bg-rose-950/60 border-rose-800 text-rose-300'
                  }`}
                >
                  {seedMessage.type === 'success' ? (
                    <CheckCircle2 className="w-3.5 h-3.5" />
                  ) : (
                    <AlertCircle className="w-3.5 h-3.5" />
                  )}
                  <span className="truncate max-w-xs">{seedMessage.text}</span>
                </div>
              )}

              {/* Seed Button */}
              <button
                type="button"
                onClick={handleSeedDatabase}
                disabled={seeding}
                className="hidden sm:flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl border border-slate-800 bg-slate-900 text-xs font-medium text-slate-300 hover:text-white hover:bg-slate-800 transition-colors disabled:opacity-50"
                title="Reset & Seed Sri Lankan Corridors into Railway DB"
              >
                <RefreshCw className={`w-3.5 h-3.5 text-indigo-400 ${seeding ? 'animate-spin' : ''}`} />
                <span className="hidden xl:inline">{seeding ? 'Seeding...' : 'Seed Railway DB'}</span>
              </button>

              {/* Density Toggle */}
              <button
                type="button"
                onClick={toggleDensity}
                className={`p-2 rounded-xl border transition-colors ${
                  density === 'compact'
                    ? 'border-waypoint-primary/40 bg-waypoint-primary/10 text-waypoint-primary'
                    : 'border-slate-800 bg-slate-900 text-slate-400 hover:text-slate-200'
                }`}
                title={`Toggle Density: Current is ${density} (Alt+D)`}
                aria-label="Toggle compact density"
              >
                <Sliders className="w-4 h-4" />
              </button>

              {/* Theme Toggle */}
              <button
                type="button"
                onClick={toggleTheme}
                className="p-2 rounded-xl border border-slate-800 bg-slate-900 text-slate-400 hover:text-slate-200 transition-colors"
                title={`Toggle Theme: Current is ${theme} (Alt+T)`}
                aria-label="Toggle color theme"
              >
                {theme === 'dark' ? <Sun className="w-4 h-4 text-amber-400" /> : <Moon className="w-4 h-4 text-indigo-400" />}
              </button>

              {/* Keyboard Shortcuts Trigger */}
              <button
                type="button"
                onClick={() => setIsShortcutsOpen(true)}
                className="p-2 rounded-xl border border-slate-800 bg-slate-900 text-slate-400 hover:text-slate-200 transition-colors"
                title="Keyboard Shortcuts Reference (?)"
                aria-label="Show keyboard shortcuts"
              >
                <HelpCircle className="w-4 h-4" />
              </button>
            </div>
          </header>

          {/* Subheader Breadcrumbs for nested navigation */}
          {breadcrumbItems.length > 0 && (
            <div className="border-b border-slate-800/80 bg-slate-950/40 px-6 py-2">
              <Breadcrumbs items={breadcrumbItems} />
            </div>
          )}

          {/* Main Page Content Viewport */}
          <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8 bg-slate-900">
            <Outlet />
          </main>
        </div>
      </div>

      {/* Global Command Palette Modal */}
      <CommandPalette
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
        onOpenShortcuts={() => {
          setIsCommandPaletteOpen(false)
          setIsShortcutsOpen(true)
        }}
      />

      {/* Keyboard Shortcuts Modal */}
      <KeyboardShortcutsModal
        isOpen={isShortcutsOpen}
        onClose={() => setIsShortcutsOpen(false)}
      />
    </div>
  )
}
