import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { Bus, LayoutGrid, Users, Star } from 'lucide-react'

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
            <span className="px-2.5 py-0.5 text-[11px] font-semibold rounded-md bg-indigo-50 text-indigo-700 border border-indigo-200 tracking-wide">
              Fleet Operations
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-bold font-display text-slate-900 tracking-tight">
            Fleet Studio, Seat Designer & Resource Feasibility
          </h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-1">
            Configure bus fleet inventory, interactive 2x2 luxury seating matrices, driver HOS rest compliance, and passenger sentiment.
          </p>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-1.5 p-1.5 bg-white border border-slate-200 rounded-2xl shadow-xs overflow-x-auto">
        {fleetTabs.map((tab) => {
          const Icon = tab.icon
          return (
            <NavLink
              key={tab.to}
              to={tab.to}
              className={({ isActive }) =>
                `flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold transition-all flex-1 justify-center ${
                  isActive
                    ? 'bg-indigo-600 text-white shadow-sm'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
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

export default FleetHubLayout
