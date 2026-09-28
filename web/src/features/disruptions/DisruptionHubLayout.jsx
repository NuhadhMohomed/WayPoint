import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { AlertTriangle, CheckSquare, Bell, Bot, ShieldAlert, GitBranch } from 'lucide-react'

const disruptionTabs = [
  { to: '/disruptions/intake', label: 'Incident Intake', icon: AlertTriangle },
  { to: '/disruptions/approvals', label: 'Manager Approval', icon: CheckSquare },
  { to: '/disruptions/alerts', label: 'Service Alerts', icon: Bell },
  { to: '/disruptions/ai-traces', label: 'AI Traces & Audits', icon: Bot },
  { to: '/disruptions/admin', label: 'Admin Console', icon: ShieldAlert },
]

export function DisruptionHubLayout() {
  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
              Component 4
            </span>
            <span className="text-xs text-slate-400 font-medium">
              Assigned to <strong className="text-white">Dineth</strong>
            </span>
          </div>
          <h1 className="text-2xl font-bold font-display text-white tracking-tight">
            Disruption, Rebooking & Approval Workbench
          </h1>
          <p className="text-xs text-slate-400 mt-1">
            Real-time transit incident intake, passenger impact analysis, manager approval gates, public alert broadcasts, and AI workflow observability.
          </p>
        </div>
        <div className="flex items-center gap-1.5 text-xs font-mono text-slate-400 bg-slate-950 px-3 py-1.5 rounded-lg border border-slate-800 self-start sm:self-auto">
          <GitBranch className="w-4 h-4 text-amber-400" />
          feature/disruption-approval
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-1 p-1 bg-slate-950 border border-slate-800 rounded-xl overflow-x-auto">
        {disruptionTabs.map((tab) => {
          const Icon = tab.icon
          return (
            <NavLink
              key={tab.to}
              to={tab.to}
              className={({ isActive }) =>
                `flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium transition-all flex-1 justify-center whitespace-nowrap ${
                  isActive
                    ? 'bg-waypoint-blue text-white shadow-md shadow-waypoint-blue/20 font-semibold'
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
