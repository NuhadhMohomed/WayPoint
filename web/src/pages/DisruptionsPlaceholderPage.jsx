import React from 'react'
import { AlertTriangle, GitBranch, Database, FileText, CheckSquare } from 'lucide-react'

export function DisruptionsPlaceholderPage() {
  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
              Component 4
            </span>
            <span className="text-xs text-slate-400 font-medium">Assigned to <strong className="text-white">Dineth</strong></span>
          </div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Disruption, Rebooking & Approval</h1>
          <p className="text-xs text-slate-400 mt-1">Disruption logging, passenger impact, multi-agent AI rebooking, and Manager Approval Workbench.</p>
        </div>
        <div className="flex items-center gap-1.5 text-xs font-mono text-slate-400 bg-slate-950 px-3 py-1.5 rounded-lg border border-slate-800">
          <GitBranch className="w-4 h-4 text-amber-400" />
          feature/c4-disruption-approval
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-amber-400 font-semibold text-xs mb-2">
            <Database className="w-4 h-4" />
            PostgreSQL Domain Entities
          </div>
          <ul className="text-xs text-slate-400 space-y-1 font-mono">
            <li>• ServiceAlerts</li>
            <li>• DisruptionCases</li>
            <li>• RebookingProposals</li>
            <li>• ApprovalDecisions</li>
            <li>• AiWorkflows & AiToolCalls</li>
          </ul>
        </div>

        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-indigo-400 font-semibold text-xs mb-2">
            <FileText className="w-4 h-4" />
            Target API Endpoints
          </div>
          <ul className="text-xs text-slate-400 space-y-1 font-mono">
            <li>• POST /api/v1/disruptions/log</li>
            <li>• POST /api/v1/disruptions/rebooking-plan</li>
            <li>• GET /api/v1/approvals/pending</li>
            <li>• POST /api/v1/approvals/decide</li>
          </ul>
        </div>

        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-rose-400 font-semibold text-xs mb-2">
            <CheckSquare className="w-4 h-4" />
            Complex Business Operation
          </div>
          <p className="text-xs text-slate-400 leading-relaxed">
            Multi-Agent AI Rebooking Evaluation with Mandatory Transport Manager Approval Gate for high-impact operational changes.
          </p>
        </div>
      </div>

      <div className="p-8 rounded-xl bg-slate-950 border border-dashed border-slate-800 text-center">
        <AlertTriangle className="w-8 h-8 text-amber-400 mx-auto mb-2 opacity-80" />
        <h3 className="text-base font-semibold text-white">Disruption & Manager Approval Workspace Ready</h3>
        <p className="text-xs text-slate-400 max-w-md mx-auto mt-1">
          Dineth can now switch to branch <code className="text-amber-300 font-mono">feature/c4-disruption-approval</code> to build the Disruption Workbench, impact charts, and Manager Approval Workbench.
        </p>
      </div>
    </div>
  )
}
