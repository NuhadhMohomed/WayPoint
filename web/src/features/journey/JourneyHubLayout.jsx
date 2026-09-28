import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { MapPin, Calendar, Compass, GitBranch } from 'lucide-react'

const journeyTabs = [
  { to: '/routes/catalog', label: 'Route & Stop Manager', icon: MapPin },
  { to: '/routes/scheduler', label: 'Service Timetables', icon: Calendar },
  { to: '/routes/corridors', label: 'Tourist Corridors', icon: Compass },
]

export function JourneyHubLayout() {
  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
              Component 1
            </span>
            <span className="text-xs text-slate-400 font-medium">
              Assigned to <strong className="text-white">Sethum</strong>
            </span>
          </div>
          <h1 className="text-2xl font-bold font-display text-white tracking-tight">
            Journey Planning & Route Catalogue
          </h1>
          <p className="text-xs text-slate-400 mt-1">
            Manage intercity corridors, intermediate stops, scheduled bus departures, and scenic Sri Lankan tourist highlights.
          </p>
        </div>
        <div className="flex items-center gap-1.5 text-xs font-mono text-slate-400 bg-slate-950 px-3 py-1.5 rounded-lg border border-slate-800">
          <GitBranch className="w-4 h-4 text-indigo-400" />
          feature/journey-planning
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-1 p-1 bg-slate-950 border border-slate-800 rounded-xl">
        {journeyTabs.map((tab) => {
          const Icon = tab.icon
          return (
            <NavLink
              key={tab.to}
              to={tab.to}
              className={({ isActive }) =>
                `flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium transition-all flex-1 justify-center ${
                  isActive
                    ? 'bg-waypoint-blue text-white shadow-md shadow-waypoint-blue/20'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/80'
                }`
              }
            >
              <Icon className="w-4 h-4" />
              {tab.label}
            </NavLink>
          )
        })}
      </div>

      {/* Active Tab Viewport */}
      <Outlet />
    </div>
  )
}

