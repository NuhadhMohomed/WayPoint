import React from 'react'
import { Bus, GitBranch, Database, FileText, CheckCircle2 } from 'lucide-react'

export function FleetPlaceholderPage() {
  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
              Component 2
            </span>
            <span className="text-xs text-slate-400 font-medium">Assigned to <strong className="text-white">Nuhadh</strong></span>
          </div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Fleet, Seat & Resource Feasibility</h1>
          <p className="text-xs text-slate-400 mt-1">Bus inventory, interactive 2x2 seat layouts, driver scheduling, and replacement solvers.</p>
        </div>
        <div className="flex items-center gap-1.5 text-xs font-mono text-slate-400 bg-slate-950 px-3 py-1.5 rounded-lg border border-slate-800">
          <GitBranch className="w-4 h-4 text-emerald-400" />
          feature/c2-fleet-resources
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-emerald-400 font-semibold text-xs mb-2">
            <Database className="w-4 h-4" />
            PostgreSQL Domain Entities
          </div>
          <ul className="text-xs text-slate-400 space-y-1 font-mono">
            <li>• Buses</li>
            <li>• SeatLayouts & Seats</li>
            <li>• Drivers & Assignments</li>
            <li>• MaintenanceRecords</li>
            <li>• Amenities & ServiceAmenities</li>
          </ul>
        </div>

        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-indigo-400 font-semibold text-xs mb-2">
            <FileText className="w-4 h-4" />
            Target API Endpoints
          </div>
          <ul className="text-xs text-slate-400 space-y-1 font-mono">
            <li>• GET /api/v1/fleet/buses</li>
            <li>• GET /api/v1/fleet/seats/{'{serviceId}'}</li>
            <li>• GET /api/v1/fleet/drivers</li>
            <li>• POST /api/v1/fleet/resources/check</li>
          </ul>
        </div>

        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-sky-400 font-semibold text-xs mb-2">
            <Bus className="w-4 h-4" />
            Complex Business Operation
          </div>
          <p className="text-xs text-slate-400 leading-relaxed">
            Real-time seat availability engine and replacement resource feasibility solver checking standby buses, seat capacity, and driver rest hours.
          </p>
        </div>
      </div>

      <div className="p-8 rounded-xl bg-slate-950 border border-dashed border-slate-800 text-center">
        <Bus className="w-8 h-8 text-emerald-400 mx-auto mb-2 opacity-80" />
        <h3 className="text-base font-semibold text-white">Fleet & Resource Workspace Ready</h3>
        <p className="text-xs text-slate-400 max-w-md mx-auto mt-1">
          Nuhadh can now switch to branch <code className="text-emerald-300 font-mono">feature/c2-fleet-resources</code> to develop bus fleet CRUD views, interactive seat templates, and driver assignment grids.
        </p>
      </div>
    </div>
  )
}
