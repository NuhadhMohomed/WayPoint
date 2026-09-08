import React from 'react'
import { Link } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { MapPin, Bus, Ticket, AlertTriangle, GitBranch, ArrowRight, ShieldCheck, CheckCircle2 } from 'lucide-react'

export function OverviewPage() {
  const { user } = useAuthStore()

  const components = [
    {
      id: 'Component 1',
      title: 'Journey Planning & Route Catalogue',
      owner: 'Sethum',
      branch: 'feature/c1-journey-planning',
      path: '/routes',
      icon: MapPin,
      color: 'indigo',
      description: 'Corridor routes, intermediate stops, sequence ordering, timetables, and candidate journey search algorithms.'
    },
    {
      id: 'Component 2',
      title: 'Fleet, Seat & Resource Feasibility',
      owner: 'Nuhadh',
      branch: 'feature/c2-fleet-resources',
      path: '/fleet',
      icon: Bus,
      color: 'emerald',
      description: 'Bus fleet inventory, 2x2 luxury seat matrices, driver scheduling, maintenance logs, and resource solvers.'
    },
    {
      id: 'Component 3',
      title: 'Booking, Ticketing & Passenger Options',
      owner: 'Mithila',
      branch: 'feature/c3-booking-ticketing',
      path: '/bookings',
      icon: Ticket,
      color: 'sky',
      description: '10-minute temporary seat holds, payment sandbox checkout, QR e-ticket issuing, and cancellation refunds.'
    },
    {
      id: 'Component 4',
      title: 'Disruption, Rebooking & Approval',
      owner: 'Dineth',
      branch: 'feature/c4-disruption-approval',
      path: '/disruptions',
      icon: AlertTriangle,
      color: 'amber',
      description: 'Disruption logging, passenger impact analysis, multi-agent AI rebooking proposals, and Transport Manager sign-off.'
    }
  ]

  return (
    <div className="space-y-8 max-w-7xl mx-auto">
      {/* Welcome Banner */}
      <div className="p-6 rounded-2xl bg-gradient-to-r from-indigo-900/60 via-slate-900 to-slate-900 border border-indigo-500/20 shadow-xl">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-indigo-500/10 border border-indigo-500/30 text-indigo-300 text-xs font-medium mb-2">
              <ShieldCheck className="w-3.5 h-3.5" />
              Shared Foundation Active
            </div>
            <h1 className="text-2xl font-bold text-white tracking-tight">
              Welcome back, {user?.fullName || 'Transit Staff'}
            </h1>
            <p className="text-sm text-slate-400 mt-1">
              Role: <span className="font-semibold text-slate-200">{user?.role}</span> • Monorepo synchronized with ASP.NET Core & Railway PostgreSQL
            </p>
          </div>
          <div className="flex items-center gap-3">
            <div className="text-right hidden sm:block">
              <div className="text-xs font-semibold text-emerald-400 flex items-center gap-1.5 justify-end">
                <CheckCircle2 className="w-4 h-4" />
                Schema Migrations Ready
              </div>
              <div className="text-[11px] text-slate-500">33 Tables • All 4 Components</div>
            </div>
          </div>
        </div>
      </div>

      {/* Component Cards Grid */}
      <div>
        <div className="mb-4">
          <h2 className="text-lg font-bold text-white tracking-tight">Team Component Workspaces</h2>
          <p className="text-xs text-slate-400">Assigned business modules for SE3090 Assignment 1</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {components.map((comp) => {
            const Icon = comp.icon
            return (
              <div
                key={comp.id}
                className="p-6 rounded-xl bg-slate-950 border border-slate-800 hover:border-slate-700 transition-all flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div className="p-2.5 rounded-lg bg-slate-900 border border-slate-800 text-indigo-400">
                      <Icon className="w-5 h-5" />
                    </div>
                    <div className="flex items-center gap-1.5 text-[11px] font-mono text-slate-400 bg-slate-900 px-2.5 py-1 rounded-md border border-slate-800">
                      <GitBranch className="w-3 h-3 text-indigo-400" />
                      {comp.branch}
                    </div>
                  </div>

                  <div className="text-xs font-semibold text-indigo-400 uppercase tracking-wider mb-1">
                    {comp.id} • Assigned to <span className="text-white font-bold">{comp.owner}</span>
                  </div>
                  <h3 className="text-base font-bold text-white mb-2">{comp.title}</h3>
                  <p className="text-xs text-slate-400 leading-relaxed mb-4">{comp.description}</p>
                </div>

                <Link
                  to={comp.path}
                  className="inline-flex items-center justify-between px-3.5 py-2 rounded-lg bg-slate-900 hover:bg-indigo-600/20 text-xs font-medium text-slate-200 hover:text-indigo-300 border border-slate-800 hover:border-indigo-500/30 transition-all"
                >
                  <span>Open Component Workspace</span>
                  <ArrowRight className="w-3.5 h-3.5" />
                </Link>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
