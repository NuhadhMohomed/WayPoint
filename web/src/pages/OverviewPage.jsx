import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { 
  Card, 
  CardHeader, 
  CardTitle, 
  CardDescription, 
  CardContent, 
  TransitBadge, 
  Button 
} from '../components/ui'
import { 
  MapPin, 
  Bus, 
  Ticket, 
  AlertTriangle, 
  GitBranch, 
  ArrowRight, 
  ShieldCheck, 
  CheckCircle2,
  Activity,
  Users,
  Compass,
  Clock,
  Radio,
  ExternalLink,
  RefreshCw
} from 'lucide-react'

export function OverviewPage() {
  const { user } = useAuthStore()
  const navigate = useNavigate()
  const [refreshing, setRefreshing] = useState(false)

  const handleRefresh = () => {
    setRefreshing(true)
    setTimeout(() => setRefreshing(false), 600)
  }

  // Telematics KPIs
  const telemetryKpis = [
    {
      title: 'Active Trips In-Transit',
      value: '24',
      change: '+3 from 08:00 SLST',
      status: 'positive',
      icon: Bus,
      subtext: 'Across 12 national corridors',
    },
    {
      title: 'On-Time Dispatch Rate',
      value: '96.4%',
      change: '+1.2% SLA compliant',
      status: 'positive',
      icon: Clock,
      subtext: 'Target >= 95.0% (NOC-SLA-01)',
    },
    {
      title: 'Fleet Operational Readiness',
      value: '38 / 42',
      change: '4 in depot maintenance',
      status: 'neutral',
      icon: Activity,
      subtext: '90.5% available capacity',
    },
    {
      title: 'Active Disruption Incidents',
      value: '1',
      change: 'Reroute active (Kandy corridor)',
      status: 'warning',
      icon: AlertTriangle,
      subtext: 'Manager approval pending',
    },
  ]

  // Live Corridor Telemetry
  const corridorTelemetry = [
    {
      code: 'EX-01',
      name: 'Southern Coastal Expressway',
      from: 'Colombo Makumbura',
      to: 'Galle / Matara',
      activeBuses: 8,
      occupancy: '88%',
      status: 'OnTime',
      statusLabel: 'Normal Dispatch',
    },
    {
      code: 'HL-04',
      name: 'Highland Mountain Corridor',
      from: 'Colombo Fort',
      to: 'Kandy / Nuwara Eliya',
      activeBuses: 6,
      occupancy: '94%',
      status: 'Disrupted',
      statusLabel: 'Monitored Delay (+25m)',
    },
    {
      code: 'TC-08',
      name: 'Scenic Ella Tea Country Odyssey',
      from: 'Kandy',
      to: 'Ella / Badulla',
      activeBuses: 4,
      occupancy: '100%',
      status: 'Available',
      statusLabel: 'Sold Out • In Route',
    },
    {
      code: 'NR-02',
      name: 'Northern Intercity Corridor',
      from: 'Colombo',
      to: 'Anuradhapura / Jaffna',
      activeBuses: 6,
      occupancy: '72%',
      status: 'OnTime',
      statusLabel: 'Normal Dispatch',
    },
  ]

  // Team component hubs
  const components = [
    {
      id: 'Component 1',
      title: 'Journey Planning & Route Catalogue',
      owner: 'Sethum',
      branch: 'feature/c1-journey-planning',
      path: '/routes',
      icon: MapPin,
      tag: 'Catalog & Scheduling',
      description: 'Corridor routes, intermediate stops, sequence ordering, timetables, and candidate journey search algorithms.'
    },
    {
      id: 'Component 2',
      title: 'Fleet, Seat & Resource Feasibility',
      owner: 'Nuhadh',
      branch: 'feature/c2-fleet-resources',
      path: '/fleet',
      icon: Bus,
      tag: 'Fleet Studio & Rostering',
      description: 'Bus fleet inventory, 2x2 luxury seat matrices, driver scheduling, maintenance logs, and resource solvers.'
    },
    {
      id: 'Component 3',
      title: 'Booking, Ticketing & Passenger Options',
      owner: 'Mithila',
      branch: 'feature/c3-booking-ticketing',
      path: '/bookings',
      icon: Ticket,
      tag: 'Ticketing & Manifest',
      description: '10-minute temporary seat holds, payment sandbox checkout, QR e-ticket issuing, and cancellation refunds.'
    },
    {
      id: 'Component 4',
      title: 'Disruption, Rebooking & Approval',
      owner: 'Dineth',
      branch: 'feature/c4-disruption-approval',
      path: '/disruptions',
      icon: AlertTriangle,
      tag: 'Disruption Desk & AI',
      description: 'Disruption logging, passenger impact analysis, multi-agent AI rebooking proposals, and Transport Manager sign-off.'
    }
  ]

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* 1. Welcome NOC Header */}
      <div className="p-6 rounded-2xl bg-gradient-to-r from-slate-900 via-slate-900 to-slate-950 border border-slate-800 shadow-xl relative overflow-hidden">
        <div className="absolute top-0 right-0 w-80 h-full bg-waypoint-primary/5 blur-3xl pointer-events-none" />

        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 relative z-10">
          <div>
            <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-waypoint-primary/10 border border-waypoint-primary/30 text-waypoint-primary text-xs font-semibold mb-2.5">
              <Radio className="w-3.5 h-3.5 animate-pulse" />
              NOC Telematics Live • SLST Active
            </div>
            <h1 className="text-2xl sm:text-3xl font-black text-white tracking-tight font-display">
              Transit Operations Cockpit
            </h1>
            <p className="text-xs sm:text-sm text-slate-400 mt-1">
              Welcome back, <span className="font-semibold text-slate-200">{user?.fullName || 'Transit Staff'}</span> ({user?.role || 'Officer'}). Monorepo synchronised with ASP.NET Core & Railway PostgreSQL.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={handleRefresh}
              className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-slate-800/80 border border-slate-700 text-xs font-medium text-slate-200 hover:bg-slate-700 transition-colors shadow-sm"
              title="Refresh Telemetry Feeds"
            >
              <RefreshCw className={`w-3.5 h-3.5 text-waypoint-primary ${refreshing ? 'animate-spin' : ''}`} />
              <span>Refresh Telemetry</span>
            </button>
            <div className="hidden sm:block text-right">
              <div className="text-xs font-bold text-waypoint-primary flex items-center gap-1.5 justify-end">
                <CheckCircle2 className="w-4 h-4" />
                Backend Synced
              </div>
              <div className="text-[11px] text-slate-500 font-mono">33 Tables • All 4 Components</div>
            </div>
          </div>
        </div>
      </div>

      {/* 2. Telemetry KPI Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {telemetryKpis.map((kpi, idx) => {
          const Icon = kpi.icon
          return (
            <Card key={idx} className="p-4 flex flex-col justify-between">
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-400">{kpi.title}</span>
                <div className={`p-2 rounded-lg ${
                  kpi.status === 'warning' ? 'bg-amber-500/10 text-amber-400' : 'bg-slate-800 text-waypoint-primary'
                }`}>
                  <Icon className="w-4 h-4" />
                </div>
              </div>

              <div>
                <div className="text-2xl font-bold font-mono tracking-tight text-slate-100">
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
      <Card className="overflow-hidden">
        <CardHeader className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800 bg-slate-900/60 pb-4">
          <div>
            <CardTitle className="text-base font-bold text-slate-100 flex items-center gap-2">
              <Compass className="w-4 h-4 text-waypoint-primary" />
              Live Corridor Operations Status
            </CardTitle>
            <CardDescription className="text-xs text-slate-400">
              National high-demand corridors and active coach assignments
            </CardDescription>
          </div>
          <Link to="/routes">
            <Button variant="ghost" size="sm" className="text-xs text-waypoint-primary">
              Open Route Catalog <ArrowRight className="w-3.5 h-3.5 ml-1" />
            </Button>
          </Link>
        </CardHeader>

        <CardContent className="p-0">
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
                      <span className="px-1.5 py-0.5 rounded bg-slate-800 text-waypoint-primary border border-slate-700 text-[11px] mr-2">
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
                      <TransitBadge status={row.status} label={row.statusLabel} />
                    </td>
                    <td className="py-3.5 px-4 text-right">
                      <button
                        type="button"
                        onClick={() => navigate('/routes')}
                        className="text-xs font-sans text-waypoint-primary hover:underline font-semibold"
                      >
                        Timetable →
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
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
                className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 hover:border-slate-700 transition-all flex flex-col justify-between shadow-lg"
              >
                <div>
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div className="p-2.5 rounded-xl bg-slate-800 border border-slate-700/80 text-waypoint-primary">
                      <Icon className="w-5 h-5" />
                    </div>
                    <div className="flex items-center gap-1.5 text-[11px] font-mono text-slate-400 bg-slate-950 px-2.5 py-1 rounded-lg border border-slate-800">
                      <GitBranch className="w-3 h-3 text-waypoint-primary" />
                      {comp.branch}
                    </div>
                  </div>

                  <div className="text-[11px] font-bold text-waypoint-primary uppercase tracking-wider mb-1">
                    {comp.id} • Lead: <span className="text-white">{comp.owner}</span>
                  </div>
                  <h3 className="text-base font-bold text-white mb-2">{comp.title}</h3>
                  <p className="text-xs text-slate-400 leading-relaxed mb-4">{comp.description}</p>
                </div>

                <Link
                  to={comp.path}
                  className="inline-flex items-center justify-between px-4 py-2.5 rounded-xl bg-slate-950 hover:bg-waypoint-primary/10 text-xs font-semibold text-slate-200 hover:text-waypoint-primary border border-slate-800 hover:border-waypoint-primary/40 transition-all"
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
