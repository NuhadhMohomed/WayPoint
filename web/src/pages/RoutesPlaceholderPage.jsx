import React from 'react'
import { MapPin, GitBranch, Database, FileText } from 'lucide-react'

export function RoutesPlaceholderPage() {
  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
              Component 1
            </span>
            <span className="text-xs text-slate-400 font-medium">Assigned to <strong className="text-white">Sethum</strong></span>
          </div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Journey Planning & Route Catalogue</h1>
          <p className="text-xs text-slate-400 mt-1">Intercity transit network, intermediate stops, timetables, and candidate search.</p>
        </div>
        <div className="flex items-center gap-1.5 text-xs font-mono text-slate-400 bg-slate-950 px-3 py-1.5 rounded-lg border border-slate-800">
          <GitBranch className="w-4 h-4 text-indigo-400" />
          feature/c1-journey-planning
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-indigo-400 font-semibold text-xs mb-2">
            <Database className="w-4 h-4" />
            PostgreSQL Domain Entities
          </div>
          <ul className="text-xs text-slate-400 space-y-1 font-mono">
            <li>• Routes</li>
            <li>• RouteStops</li>
            <li>• BoardingPoints</li>
            <li>• TouristDestinations</li>
            <li>• Services & FareRules</li>
          </ul>
        </div>

        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-emerald-400 font-semibold text-xs mb-2">
            <FileText className="w-4 h-4" />
            Target API Endpoints
          </div>
          <ul className="text-xs text-slate-400 space-y-1 font-mono">
            <li>• GET /api/v1/routes</li>
            <li>• POST /api/v1/routes</li>
            <li>• GET /api/v1/services</li>
            <li>• POST /api/v1/journey/search</li>
          </ul>
        </div>

        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-sky-400 font-semibold text-xs mb-2">
            <MapPin className="w-4 h-4" />
            Complex Business Operation
          </div>
          <p className="text-xs text-slate-400 leading-relaxed">
            Preference-Aware Candidate Journey & Transfer Window Generator (enforcing minimum 20-minute transfer window).
          </p>
        </div>
      </div>

      <div className="p-8 rounded-xl bg-slate-950 border border-dashed border-slate-800 text-center">
        <MapPin className="w-8 h-8 text-indigo-400 mx-auto mb-2 opacity-80" />
        <h3 className="text-base font-semibold text-white">Route Catalogue Workspace Ready</h3>
        <p className="text-xs text-slate-400 max-w-md mx-auto mt-1">
          Sethum can now switch to branch <code className="text-indigo-300 font-mono">feature/c1-journey-planning</code> and build Route CRUD management views, stop sequences, and timetable scheduling.
        </p>
      </div>
    </div>
  )
}
