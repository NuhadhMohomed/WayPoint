import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { MapPin, Calendar, Compass, GitBranch, Sparkles } from 'lucide-react'

const journeyTabs = [
  { to: '/routes/catalog', label: 'Route Catalog & Stops', icon: MapPin },
  { to: '/routes/scheduler', label: 'Service Timetables', icon: Calendar },
  { to: '/routes/corridors', label: 'Scenic Tourist Corridors', icon: Compass },
]

export function JourneyHubLayout() {
  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-bold rounded-md bg-waypoint-primary/20 text-waypoint-primary border border-waypoint-primary/30 uppercase tracking-wide">
              Component 1
            </span>
            <span className="text-xs text-slate-400 font-medium">
              Lead Engineer: <strong className="text-white">Sethum</strong>
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black font-display text-white tracking-tight">
            Journey Planning & Route Catalogue
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Manage intercity corridors, intermediate stops, scheduled bus departures, and scenic Sri Lankan tourist highlights.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 text-xs font-mono text-slate-300 bg-slate-950 px-3 py-1.5 rounded-xl border border-slate-800 shadow-inner">
            <GitBranch className="w-3.5 h-3.5 text-waypoint-primary" />
            <span>feature/c1-journey-planning</span>
          </div>
        </div>
      </div>

      {/* Tab Navigation Bar */}
      <div className="flex items-center gap-1.5 p-1.5 bg-slate-950 border border-slate-800 rounded-2xl shadow-lg">
        {journeyTabs.map((tab) => {
          const Icon = tab.icon
          return (
            <NavLink
              key={tab.to}
              to={tab.to}
              className={({ isActive }) =>
                `flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold transition-all flex-1 justify-center ${
                  isActive
                    ? 'bg-waypoint-primary text-waypoint-onPrimary shadow-md shadow-waypoint-primary/10'
                    : 'text-slate-400 hover:text-slate-100 hover:bg-slate-900'
                }`
              }
            >
              <Icon className="w-4 h-4 flex-shrink-0" />
              <span>{tab.label}</span>
            </NavLink>
          )
        })}
      </div>

      {/* Active Tab Viewport */}
      <div className="transition-all">
        <Outlet />
      </div>
    </div>
  )
}
