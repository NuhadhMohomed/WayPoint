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
  Calendar,
  RefreshCw,
  ArrowRight,
  TrendingUp,
  FileSpreadsheet,
  Activity
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

  const operationsModules = [
    {
      id: 'routes',
      category: 'Network & Scheduling',
      title: 'Journey Planning & Route Catalogue',
      description: 'Interactive route stops, corridor highlights, departure schedules, and multi-criteria journey candidate generation.',
      path: '/routes/catalog',
      tag: 'Route Manager',
      icon: Calendar,
    },
    {
      id: 'fleet',
      category: 'Fleet & Assets',
      title: 'Fleet, Seat & Resource Management',
      description: 'Bus fleet matrix, visual 2D seat layout designer, driver rostering with rest-hour compliance, and passenger sentiment.',
      path: '/fleet/buses',
      tag: 'Fleet Hub',
      icon: Bus,
    },
    {
      id: 'bookings',
      category: 'Passenger & Revenue',
      title: 'Booking, Ticketing & Passenger Manifest',
      description: 'Real-time seat holds, automated payment processing, live passenger manifest monitor, and dispatch exports.',
      path: '/bookings',
      tag: 'Manifest Monitor',
      icon: FileSpreadsheet,
    },
    {
      id: 'disruptions',
      category: 'Operations & Alerts',
      title: 'Disruption Handling & Incident Response',
      description: 'Disruption intake, Transport Manager approval workflows, public service alert broadcasts, and automated passenger rebooking.',
      path: '/disruptions/intake',
      tag: 'Incident Desk',
      icon: AlertTriangle,
    },
  ]

  return (
    <div className="space-y-6 text-slate-800">
      {/* 1. Header Banner */}
      <div className="rounded-2xl bg-white border border-slate-200/80 p-6 shadow-xs">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200 font-mono">
                LIVE TELEMATICS
              </span>
              <span className="text-xs text-slate-500 font-mono">NOC Node: LK-CMB-01</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-900 font-display">
              Transit Operations Cockpit
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              National high-demand transit dispatch, live corridor occupancy, and fleet coordination.
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
              <div className="text-xs font-bold text-emerald-600 flex items-center gap-1.5 justify-end">
                <CheckCircle2 className="w-4 h-4" />
                Backend Synced
              </div>
              <div className="text-[11px] text-slate-400 font-mono">All Systems Operational</div>
            </div>
          </div>
        </div>
      </div>

      {/* 2. Telemetry KPI Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {telemetryKpis.map((kpi, idx) => {
          const Icon = kpi.icon
          return (
            <Card key={idx} className="p-4 flex flex-col justify-between bg-white border-slate-200/80 shadow-xs">
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-500">{kpi.title}</span>
                <div className={`p-2 rounded-lg ${
                  kpi.status === 'warning' ? 'bg-amber-50 text-amber-600' : 'bg-indigo-50 text-indigo-600'
                }`}>
                  <Icon className="w-4 h-4" />
                </div>
              </div>

              <div>
                <div className="text-2xl font-bold font-mono tracking-tight text-slate-900">
                  {kpi.value}
                </div>
                <div className="mt-1 flex items-center justify-between text-[11px]">
                  <span className={kpi.status === 'warning' ? 'text-amber-600 font-semibold' : 'text-emerald-600 font-semibold'}>
                    {kpi.change}
                  </span>
                </div>
                <div className="text-[10px] text-slate-400 mt-1 truncate">
                  {kpi.subtext}
                </div>
              </div>
            </Card>
          )
        })}
      </div>

      {/* 3. Live Corridor Telemetry Table */}
      <Card className="overflow-hidden bg-white border-slate-200/80 shadow-xs">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 bg-white p-4">
          <div>
            <h3 className="text-base font-bold text-slate-900 flex items-center gap-2">
              <Compass className="w-4 h-4 text-indigo-600" />
              Live Corridor Operations Status
            </h3>
            <p className="text-xs text-slate-500">
              National high-demand corridors and active coach assignments
            </p>
          </div>
          <Link to="/routes/catalog">
            <Button variant="ghost" size="sm" className="text-xs text-indigo-600 hover:text-indigo-700">
              Open Route Catalog <ArrowRight className="w-3.5 h-3.5 ml-1" />
            </Button>
          </Link>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="border-b border-slate-200 bg-slate-50/80 font-semibold text-slate-600">
              <tr>
                <th className="py-3 px-4">Corridor</th>
                <th className="py-3 px-4">Route Path</th>
                <th className="py-3 px-4 text-center">Active Coaches</th>
                <th className="py-3 px-4 text-center">Load Factor</th>
                <th className="py-3 px-4 text-center">Dispatch Status</th>
                <th className="py-3 px-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 font-mono text-slate-700">
              {corridorTelemetry.map((row, idx) => (
                <tr key={idx} className="hover:bg-slate-50/80 transition-colors">
                  <td className="py-3.5 px-4 font-bold text-slate-900">
                    <span className="px-1.5 py-0.5 rounded bg-indigo-50 text-indigo-700 border border-indigo-100 text-[11px] mr-2">
                      {row.code}
                    </span>
                    <span className="font-sans font-medium text-slate-800">{row.name}</span>
                  </td>
                  <td className="py-3.5 px-4 font-sans text-slate-600">
                    {row.from} <span className="text-slate-400">→</span> {row.to}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold text-slate-800">
                    {row.activeBuses}
                  </td>
                  <td className="py-3.5 px-4 text-center font-bold">
                    <span className={parseInt(row.occupancy) > 90 ? 'text-amber-600' : 'text-emerald-600'}>
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
                      className="text-xs font-sans text-indigo-600 hover:text-indigo-800 hover:underline font-semibold"
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

      {/* 4. Operational Command Hubs Grid */}
      <div>
        <div className="mb-4">
          <h2 className="text-lg font-bold text-slate-900 tracking-tight font-display">
            Operational Hubs
          </h2>
          <p className="text-xs text-slate-500">Core operational modules for transit dispatch, fleet management, ticketing, and incident response.</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {operationsModules.map((module) => {
            const Icon = module.icon
            return (
              <div
                key={module.id}
                className="p-5 rounded-2xl bg-white border border-slate-200/80 hover:border-indigo-300 hover:shadow-md transition-all flex flex-col justify-between shadow-xs"
              >
                <div>
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div className="p-2.5 rounded-xl bg-indigo-50 border border-indigo-100 text-indigo-600">
                      <Icon className="w-5 h-5" />
                    </div>
                    <span className="text-[11px] font-semibold text-indigo-600 bg-indigo-50/80 px-2.5 py-0.5 rounded-full border border-indigo-100">
                      {module.category}
                    </span>
                  </div>

                  <h3 className="text-base font-bold text-slate-900 mb-1.5">{module.title}</h3>
                  <p className="text-xs text-slate-500 leading-relaxed mb-4">{module.description}</p>
                </div>

                <Link
                  to={module.path}
                  className="inline-flex items-center justify-between px-4 py-2.5 rounded-xl bg-slate-50 hover:bg-indigo-50 text-xs font-semibold text-slate-700 hover:text-indigo-700 border border-slate-200 hover:border-indigo-200 transition-all"
                >
                  <span>Launch {module.tag}</span>
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
