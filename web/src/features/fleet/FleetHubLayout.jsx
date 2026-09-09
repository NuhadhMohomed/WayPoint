import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { Bus, LayoutGrid, Users, GitBranch } from 'lucide-react'

const fleetTabs = [
  { to: '/fleet/buses', label: 'Bus Fleet', icon: Bus },
  { to: '/fleet/layouts', label: 'Seat Layouts', icon: LayoutGrid },
  { to: '/fleet/drivers', label: 'Driver Roster', icon: Users },
]

export function FleetHubLayout() {
  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
              Component 2
            </span>
            <span className="text-xs text-slate-400 font-medium">
              Assigned to <strong className="text-white">Nuhadh</strong>
            </span>
          </div>
          <h1 className="text-2xl font-bold font-display text-white tracking-tight">
            Fleet, Seat & Resource Feasibility
          </h1>
          <p className="text-xs text-slate-400 mt-1">
            Bus inventory, interactive seat layouts, driver scheduling, and replacement resource solvers.
          </p>
        </div>
        <div className="flex items-center gap-1.5 text-xs font-mono text-slate-400 bg-slate-950 px-3 py-1.5 rounded-lg border border-slate-800">
          <GitBranch className="w-4 h-4 text-emerald-400" />
          feature/fleet-feasibility
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-1 p-1 bg-slate-950 border border-slate-800 rounded-xl">
        {fleetTabs.map((tab) => {
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

      {/* Active Tab Content */}
      <Outlet />
    </div>
  )
}
