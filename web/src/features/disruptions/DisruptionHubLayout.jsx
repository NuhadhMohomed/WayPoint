import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { AlertTriangle, CheckSquare, Bell, Bot, ShieldAlert, GitBranch } from 'lucide-react'

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
            <span className="px-2 py-0.5 text-[10px] font-bold rounded-md bg-waypoint-primary/20 text-waypoint-primary border border-waypoint-primary/30 uppercase tracking-wide">
              Component 4
            </span>
            <span className="text-xs text-slate-400 font-medium">
              Lead Engineer: <strong className="text-white">Dineth</strong>
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black font-display text-white tracking-tight">
            Disruption Desk, Approvals & AI Observability
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Real-time incident intake, impact solvers, Transport Manager approval gates, public alert broadcasts, and AI workflow telemetry.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 text-xs font-mono text-slate-300 bg-slate-950 px-3 py-1.5 rounded-xl border border-slate-800 shadow-inner">
            <GitBranch className="w-3.5 h-3.5 text-waypoint-primary" />
            <span>feature/c4-disruption-approval</span>
          </div>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-1.5 p-1.5 bg-slate-950 border border-slate-800 rounded-2xl shadow-lg overflow-x-auto">
        {disruptionTabs.map((tab) => {
          const Icon = tab.icon
          return (
            <NavLink
              key={tab.to}
              to={tab.to}
              className={({ isActive }) =>
                `flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold transition-all flex-1 justify-center whitespace-nowrap ${
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
