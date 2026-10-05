import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { Bus, LayoutGrid, Users, GitBranch, Star } from 'lucide-react'

const fleetTabs = [
  { to: '/fleet/buses', label: 'Bus Fleet Matrix', icon: Bus },
  { to: '/fleet/layouts', label: 'Seat Layout Designer', icon: LayoutGrid },
  { to: '/fleet/drivers', label: 'Crew & Driver Roster', icon: Users },
  { to: '/fleet/reviews', label: 'Fleet Reviews & Sentiment', icon: Star },
]

export function FleetHubLayout() {
  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-bold rounded-md bg-waypoint-primary/20 text-waypoint-primary border border-waypoint-primary/30 uppercase tracking-wide">
              Component 2
            </span>
            <span className="text-xs text-slate-400 font-medium">
              Lead Engineer: <strong className="text-white">Nuhadh</strong>
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black font-display text-white tracking-tight">
            Fleet Studio, Seat Designer & Resource Feasibility
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Configure bus fleet inventory, interactive 2x2 luxury seating matrices, driver HOS rest compliance, and passenger sentiment.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 text-xs font-mono text-slate-300 bg-slate-950 px-3 py-1.5 rounded-xl border border-slate-800 shadow-inner">
            <GitBranch className="w-3.5 h-3.5 text-waypoint-primary" />
            <span>feature/c2-fleet-resources</span>
          </div>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-1.5 p-1.5 bg-slate-950 border border-slate-800 rounded-2xl shadow-lg">
        {fleetTabs.map((tab) => {
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

      {/* Active Tab Content */}
      <div className="transition-all">
        <Outlet />
      </div>
    </div>
  )
}
