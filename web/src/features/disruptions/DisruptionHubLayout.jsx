import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { AlertTriangle, CheckSquare, Bell, Bot, ShieldAlert } from 'lucide-react'

const disruptionTabs = [
  { to: '/disruptions/intake', label: 'Incident Intake Desk', icon: AlertTriangle },
  { to: '/disruptions/approvals', label: 'Manager Approvals', icon: CheckSquare },
  { to: '/disruptions/alerts', label: 'Broadcast Alerts', icon: Bell },
  { to: '/disruptions/ai-traces', label: 'AI Observability & Traces', icon: Bot },
  { to: '/disruptions/admin', label: 'Admin & Audit Vault', icon: ShieldAlert },
]

export function DisruptionHubLayout() {
  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2.5 py-0.5 text-[11px] font-semibold rounded-md bg-indigo-50 text-indigo-700 border border-indigo-200 tracking-wide">
              Incident Management
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-bold font-display text-slate-900 tracking-tight">
            Disruption Desk, Approvals & AI Observability
          </h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-1">
            Real-time incident intake, impact solvers, Transport Manager approval gates, public alert broadcasts, and AI workflow telemetry.
          </p>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-1.5 p-1.5 bg-white border border-slate-200 rounded-2xl shadow-xs overflow-x-auto">
        {disruptionTabs.map((tab) => {
          const Icon = tab.icon
          return (
            <NavLink
              key={tab.to}
              to={tab.to}
              className={({ isActive }) =>
                `flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold transition-all flex-1 justify-center whitespace-nowrap ${
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

export default DisruptionHubLayout
