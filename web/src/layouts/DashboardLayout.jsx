import React, { useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { devApi } from '../api/client'
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
  AlertCircle
} from 'lucide-react'

export function DashboardLayout() {
  const { user, logout } = useAuthStore()
  const navigate = useNavigate()
  const [seeding, setSeeding] = useState(false)
  const [seedMessage, setSeedMessage] = useState(null)

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const handleSeedDatabase = async () => {
    try {
      setSeeding(true)
      setSeedMessage(null)
      const res = await devApi.seedDatabase()
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
      label: 'Overview',
      icon: LayoutDashboard,
      roles: ['Admin', 'TransportManager', 'Operator', 'Passenger']
    },
    {
      to: '/routes',
      label: 'Routes & Timetables',
      subtext: 'Sethum • Component 1',
      icon: MapPin,
      roles: ['Admin', 'TransportManager', 'Operator']
    },
    {
      to: '/fleet',
      label: 'Fleet & Seat Maps',
      subtext: 'Nuhadh • Component 2',
      icon: Bus,
      roles: ['Admin', 'TransportManager', 'Operator']
    },
    {
      to: '/bookings',
      label: 'Bookings & Ticketing',
      subtext: 'Mithila • Component 3',
      icon: Ticket,
      roles: ['Admin', 'TransportManager', 'Operator', 'Passenger']
    },
    {
      to: '/disruptions',
      label: 'Disruption & Approvals',
      subtext: 'Dineth • Component 4',
      icon: AlertTriangle,
      roles: ['Admin', 'TransportManager', 'Operator']
    },
  ]

  const userRole = user?.role || 'Passenger'
  const filteredNav = navItems.filter(item => item.roles.includes(userRole))

  return (
    <div className="flex h-screen bg-slate-900 text-slate-100 antialiased overflow-hidden">
      {/* Sidebar */}
      <aside className="w-64 flex-shrink-0 bg-slate-950 border-r border-slate-800 flex flex-col justify-between">
        <div>
          {/* Logo */}
          <div className="h-16 flex items-center px-6 border-b border-slate-800 gap-3">
            <div className="w-9 h-9 rounded-lg bg-indigo-600 flex items-center justify-center text-white shadow-lg shadow-indigo-500/30">
              <Compass className="w-5 h-5 animate-pulse" />
            </div>
            <div>
              <span className="font-bold text-lg tracking-tight text-white">WayPoint</span>
              <span className="block text-[10px] text-indigo-400 font-medium tracking-wider uppercase">Transit Platform</span>
            </div>
          </div>

          {/* Navigation Links */}
          <nav className="p-4 space-y-1.5">
            {filteredNav.map((item) => {
              const Icon = item.icon
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    `flex items-center gap-3 px-3.5 py-2.5 rounded-lg text-sm font-medium transition-all ${
                      isActive
                        ? 'bg-indigo-600/20 text-indigo-400 border border-indigo-500/30 shadow-sm'
                        : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/80'
                    }`
                  }
                >
                  <Icon className="w-4 h-4 flex-shrink-0" />
                  <div className="flex-1 truncate">
                    <div className="truncate">{item.label}</div>
                    {item.subtext && (
                      <div className="text-[10px] text-slate-500 font-normal truncate">{item.subtext}</div>
                    )}
                  </div>
                </NavLink>
              )
            })}
          </nav>
        </div>

        {/* User Card & Logout */}
        <div className="p-4 border-t border-slate-800 bg-slate-950/60">
          <div className="flex items-center justify-between gap-3 mb-3">
            <div className="flex items-center gap-2.5 truncate">
              <div className="w-8 h-8 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center text-indigo-400 font-bold text-xs uppercase">
                {user?.fullName ? user.fullName[0] : 'U'}
              </div>
              <div className="truncate">
                <div className="text-xs font-semibold text-slate-200 truncate">{user?.fullName || 'User'}</div>
                <span className="inline-block px-1.5 py-0.5 text-[9px] font-medium rounded bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
                  {userRole}
                </span>
              </div>
            </div>
            <button
              onClick={handleLogout}
              title="Log out"
              className="p-1.5 rounded-md text-slate-400 hover:text-rose-400 hover:bg-slate-900 transition-colors"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top Header */}
        <header className="h-16 bg-slate-950/80 backdrop-blur border-b border-slate-800 px-8 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <span className="text-xs text-slate-400 bg-slate-900 px-2.5 py-1 rounded-full border border-slate-800 flex items-center gap-1.5">
              <span className="w-2 h-2 rounded-full bg-emerald-500"></span>
              API: ASP.NET Core 8
            </span>
            <span className="text-xs text-slate-400 bg-slate-900 px-2.5 py-1 rounded-full border border-slate-800 flex items-center gap-1.5">
              <Database className="w-3 h-3 text-indigo-400" />
              Railway PostgreSQL
            </span>
          </div>

          <div className="flex items-center gap-3">
            {seedMessage && (
              <div className={`text-xs px-3 py-1 rounded border flex items-center gap-1.5 ${
                seedMessage.type === 'success' 
                  ? 'bg-emerald-950/60 border-emerald-800 text-emerald-300' 
                  : 'bg-rose-950/60 border-rose-800 text-rose-300'
              }`}>
                {seedMessage.type === 'success' ? <CheckCircle2 className="w-3.5 h-3.5" /> : <AlertCircle className="w-3.5 h-3.5" />}
                {seedMessage.text}
              </div>
            )}
            <button
              onClick={handleSeedDatabase}
              disabled={seeding}
              className="px-3 py-1.5 text-xs font-medium rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white flex items-center gap-1.5 transition-colors disabled:opacity-50"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${seeding ? 'animate-spin' : ''}`} />
              {seeding ? 'Seeding...' : 'Seed Railway DB'}
            </button>
          </div>
        </header>

        {/* Page Content Viewport */}
        <main className="flex-1 overflow-y-auto p-8 bg-slate-900">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
