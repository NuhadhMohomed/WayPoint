import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { TransitBadge } from '../components/ui/TransitBadge'
import {
  Compass,
  Bus,
  CheckCircle2,
  AlertTriangle,
  Users,
  Calendar,
  Clock,
  RefreshCw,
  GitBranch,
  ArrowRight,
  TrendingUp,
  FileSpreadsheet,
  Activity,
  Layers
} from 'lucide-react'

export function OverviewPage() {
  const [refreshing, setRefreshing] = useState(false)
  const navigate = useNavigate()

  const handleRefresh = () => {
    setRefreshing(true)
    setTimeout(() => setRefreshing(false), 600)
  }

  const telemetryKpis = [
    {
      title: 'Active Trips In-Transit',
      value: '24',
      change: '+3 departures in last hour',
      subtext: 'Colombo, Kandy, Galle Expressways',
      icon: Activity,
      status: 'normal',
    },
    {
      title: 'Fleet Availability Rate',
      value: '91.8%',
      change: '45 Active / 4 Maintenance',
      subtext: 'Luxury & Standard coaches',
      icon: Bus,
      status: 'normal',
    },
    {
      title: 'Active Disruption Alerts',
      value: '2',
      change: '1 AI Rebooking Active',
      subtext: 'Weather warning: Nuwara Eliya',
      icon: AlertTriangle,
      status: 'warning',
    },
    {
      title: 'On-Time Dispatch Rate',
      value: '98.2%',
      change: '24 departures on-time today',
      subtext: 'Target SLA >= 95.0%',
      icon: TrendingUp,
      status: 'normal',
    },
  ]

  const corridorTelemetry = [
    {
      code: 'EX-01',
      name: 'Southern Coastal Expressway',
      from: 'Colombo Fort (Makumbura)',
      to: 'Galle / Matara',
      activeBuses: 8,
      occupancy: '94%',
      status: 'luxury',
      statusLabel: 'Operating Normal',
    },
    {
      code: 'CC-02',
      name: 'Central Highlands Corridor',
      from: 'Colombo Central',
      to: 'Kandy Goods Shed',
      activeBuses: 6,
      occupancy: '88%',
      status: 'express',
      statusLabel: 'On Time',
    },
    {
      code: 'TC-04',
      name: 'Tea Country Scenic Route',
      from: 'Colombo Fort',
      to: 'Ella / Badulla',
      activeBuses: 4,
      occupancy: '96%',
      status: 'held',
      statusLabel: 'High Demand',
    },
  ]

  const components = [
    {
      id: 'Component 1',
      title: 'Journey Planning & Route Catalogue',
      owner: 'Sethum',
      branch: 'feature/journey-planning',
      description: 'Interactive route stops, corridor highlights, departure schedules, and multi-criteria journey candidate generation.',
      path: '/routes/catalog',
      tag: 'Route Manager',
      icon: Calendar,
    },
    {
      id: 'Component 2',
      title: 'Fleet, Seat & Resource Feasibility',
      owner: 'Nuhadh',
      branch: 'feature/fleet-feasibility',
      description: 'Bus fleet matrix, visual 2D seat layout designer, driver rostering with rest-hour compliance, and customer feedback.',
      path: '/fleet/buses',
      tag: 'Fleet Hub',
      icon: Bus,
    },
    {
      id: 'Component 3',
      title: 'Booking, Ticketing & Passenger Manifest',
      owner: 'Mithila',
      branch: 'feature/booking-ticketing',
      description: 'Temporary 10m seat hold reservations, simulated payment sandbox, live passenger manifest monitor, and CSV/PDF export.',
      path: '/bookings',
      tag: 'Booking Monitor',
      icon: FileSpreadsheet,
    },
    {
      id: 'Component 4',
      title: 'Disruption, Rebooking & AI Ops',
      owner: 'Dineth',
      branch: 'feature/disruption-management',
      description: 'Disruption intake, Manager Approval Workbench, AI multi-agent execution telemetry, and network alert broadcasts.',
      path: '/disruptions/intake',
      tag: 'Disruption Desk',
      icon: AlertTriangle,
    },
  ]

  return (
    <div className="space-y-6 text-slate-100">
      {/* 1. Header Banner */}
      <div className="rounded-2xl bg-slate-900 border border-slate-800 p-6 shadow-sm">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-indigo-600/20 text-indigo-400 border border-indigo-500/30 font-mono">
                LIVE TELEMATICS
              </span>
              <span className="text-xs text-slate-400 font-mono">NOC Node: LK-CMB-01</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-white font-display">
              Transit Operations Cockpit
            </h1>
            <p className="text-xs text-slate-400 mt-1">
              National high-demand transit dispatch, live corridor occupancy, and multi-agent coordination.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <Button
              variant="outline"
              size="sm"
              onClick={handleRefresh}
              icon={RefreshCw}
              isLoading={refreshing}
            >
              Refresh Telemetry
            </Button>
            <div className="hidden sm:block text-right">
              <div className="text-xs font-bold text-emerald-400 flex items-center gap-1.5 justify-end">
                <CheckCircle2 className="w-4 h-4" />
                Backend Synced
              </div>
              <div className="text-[11px] text-slate-500 font-mono">All 4 Components Online</div>
            </div>
          </div>
        </div>
      </div>

      {/* 2. Telemetry KPI Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {telemetryKpis.map((kpi, idx) => {
          const Icon = kpi.icon
          return (
            <Card key={idx} className="p-4 flex flex-col justify-between bg-slate-900 border-slate-800">
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-400">{kpi.title}</span>
                <div className={`p-2 rounded-lg ${
                  kpi.status === 'warning' ? 'bg-amber-500/10 text-amber-400' : 'bg-slate-800 text-indigo-400'
                }`}>
                  <Icon className="w-4 h-4" />
                </div>
              </div>

              <div>
                <div className="text-2xl font-bold font-mono tracking-tight text-white">
                  {kpi.value}
                </div>
                <div className="mt-1 flex items-center justify-between text-[11px]">
                  <span className={kpi.status === 'warning' ? 'text-amber-400 font-semibold' : 'text-emerald-400 font-semibold'}>
                    {kpi.change}
                  </span>
                </div>
                <div className="text-[10px] text-slate-500 mt-1 truncate">
                  {kpi.subtext}
                </div>
              </div>
            </Card>
          )
        })}
      </div>

      {/* 3. Live Corridor Telemetry Table */}
      <Card className="overflow-hidden bg-slate-900 border-slate-800">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800 bg-slate-900/60 p-4">
          <div>
            <h3 className="text-base font-bold text-white flex items-center gap-2">
              <Compass className="w-4 h-4 text-indigo-400" />
              Live Corridor Operations Status
            </h3>
            <p className="text-xs text-slate-400">
              National high-demand corridors and active coach assignments
            </p>
          </div>
          <Link to="/routes/catalog">
            <Button variant="ghost" size="sm" className="text-xs text-indigo-400">
              Open Route Catalog <ArrowRight className="w-3.5 h-3.5 ml-1" />
            </Button>
          </Link>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="border-b border-slate-800 bg-slate-950/80 font-semibold text-slate-400">
              <tr>
                <th className="py-3 px-4">Corridor</th>
                <th className="py-3 px-4">Route Path</th>
                <th className="py-3 px-4 text-center">Active Coaches</th>
                <th className="py-3 px-4 text-center">Load Factor</th>
                <th className="py-3 px-4 text-center">Dispatch Status</th>
                <th className="py-3 px-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/80 font-mono">
              {corridorTelemetry.map((row, idx) => (
                <tr key={idx} className="hover:bg-slate-800/30 transition-colors">
                  <td className="py-3.5 px-4 font-bold text-slate-200">
                    <span className="px-1.5 py-0.5 rounded bg-slate-800 text-indigo-400 border border-slate-700 text-[11px] mr-2">
                      {row.code}
                    </span>
                    <span className="font-sans font-medium text-slate-300">{row.name}</span>
                  </td>
                  <td className="py-3.5 px-4 font-sans text-slate-400">
                    {row.from} <span className="text-slate-600">→</span> {row.to}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold text-slate-300">
                    {row.activeBuses}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold">
                    <span className={parseInt(row.occupancy) > 90 ? 'text-amber-400' : 'text-emerald-400'}>
                      {row.occupancy}
                    </span>
                  </td>
                  <td className="py-3.5 px-4 text-center">
                    <TransitBadge variant={row.status}>{row.statusLabel}</TransitBadge>
                  </td>
                  <td className="py-3.5 px-4 text-right">
                    <button
                      type="button"
                      onClick={() => navigate('/routes/catalog')}
                      className="text-xs font-sans text-indigo-400 hover:underline font-semibold"
                    >
                      Timetable →
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {/* 4. Component Workspaces Grid */}
      <div>
        <div className="mb-4">
          <h2 className="text-lg font-bold text-white tracking-tight font-display">
            Modular Component Workspaces
          </h2>
          <p className="text-xs text-slate-400">Assigned business modules for SE3090 Assignment 1</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {components.map((comp) => {
            const Icon = comp.icon
            return (
              <div
                key={comp.id}
                className="p-5 rounded-2xl bg-slate-900 border border-slate-800 hover:border-slate-700 transition-all flex flex-col justify-between shadow-lg"
              >
                <div>
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div className="p-2.5 rounded-xl bg-slate-800 border border-slate-700/80 text-indigo-400">
                      <Icon className="w-5 h-5" />
                    </div>
                    <div className="flex items-center gap-1.5 text-[11px] font-mono text-slate-400 bg-slate-950 px-2.5 py-1 rounded-lg border border-slate-800">
                      <GitBranch className="w-3 h-3 text-indigo-400" />
                      {comp.branch}
                    </div>
                  </div>

                  <div className="text-[11px] font-bold text-indigo-400 uppercase tracking-wider mb-1">
                    {comp.id} • Lead: <span className="text-white">{comp.owner}</span>
                  </div>
                  <h3 className="text-base font-bold text-white mb-2">{comp.title}</h3>
                  <p className="text-xs text-slate-400 leading-relaxed mb-4">{comp.description}</p>
                </div>

                <Link
                  to={comp.path}
                  className="inline-flex items-center justify-between px-4 py-2.5 rounded-xl bg-slate-950 hover:bg-indigo-600/10 text-xs font-semibold text-slate-200 hover:text-indigo-400 border border-slate-800 hover:border-indigo-500/40 transition-all"
                >
                  <span>Launch {comp.tag}</span>
                  <ArrowRight className="w-4 h-4" />
                </Link>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
export default OverviewPage
