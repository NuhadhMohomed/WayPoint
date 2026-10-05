import React from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { Users, ShieldAlert, ShieldCheck } from 'lucide-react'

const adminTabs = [
  { to: '/admin/users', label: 'User Directory & RBAC', icon: Users },
  { to: '/admin/audit', label: 'Audit & Security Vault', icon: ShieldAlert },
]

export function AdminHubLayout() {
  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-bold rounded-md bg-purple-500/20 text-purple-400 border border-purple-500/30 uppercase tracking-wide">
              Administration
            </span>
            <span className="text-xs text-slate-400 font-medium">
              Authority: <strong className="text-white">System Governance & RBAC</strong>
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black font-display text-white tracking-tight flex items-center gap-2.5">
            <ShieldCheck className="w-7 h-7 text-waypoint-primary" />
            Admin Console & User Governance
          </h1>
          <p className="text-xs sm:text-sm text-slate-400 mt-1">
            Centrally manage user accounts, assign transit roles, enforce security policies, unlock restricted accounts, and audit immutable security logs.
          </p>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex items-center gap-1.5 p-1.5 bg-slate-950 border border-slate-800 rounded-2xl shadow-lg overflow-x-auto">
        {adminTabs.map((tab) => {
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

      {/* Subpage Content */}
      <Outlet />
    </div>
  )
}
