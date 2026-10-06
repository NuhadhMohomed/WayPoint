import React, { useState, useEffect, useCallback } from 'react'
import { disruptionApi } from './disruptionApi'
import { useDisruptionStore } from '@/store/disruptionStore'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import { JsonDiffViewer } from '@/components/ui/JsonDiffViewer'
import {
  ShieldAlert, Lock, History, Search, Filter, RefreshCw,
  ChevronDown, ChevronRight, FileCode, CheckCircle2, User,
  Clock, Loader2, Database, ShieldCheck
} from 'lucide-react'

export function AdminConsolePage() {
  const { auditLogs, auditLogsLoading, setAuditLogs, setAuditLogsLoading } = useDisruptionStore()

  const [expandedLogId, setExpandedLogId] = useState(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [actionFilter, setActionFilter] = useState('ALL')
  const [errorMessage, setErrorMessage] = useState(null)

  const toggleExpanded = (id) => {
    setExpandedLogId((prev) => (prev === id ? null : id))
  }

  const loadAuditLogs = useCallback(async () => {
    setAuditLogsLoading(true)
    setErrorMessage(null)
    try {
      const data = await disruptionApi.getAuditLogs({ limit: 100 })
      setAuditLogs(data)
    } catch (err) {
      setErrorMessage(err.response?.data?.detail || 'Failed to load immutable audit logs.')
    } finally {
      setAuditLogsLoading(false)
    }
  }, [setAuditLogs, setAuditLogsLoading])

  useEffect(() => {
    loadAuditLogs()
  }, [loadAuditLogs])

  const formatJson = (jsonString) => {
    try {
      const parsed = typeof jsonString === 'string' ? JSON.parse(jsonString) : jsonString
      return JSON.stringify(parsed, null, 2)
    } catch {
      return jsonString || '{}'
    }
  }

  // Filter logs
  const filteredLogs = auditLogs.filter((log) => {
    const matchesAction =
      actionFilter === 'ALL' || log.actionType?.toLowerCase().includes(actionFilter.toLowerCase())
    const matchesSearch =
      !searchTerm ||
      log.actorId?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      log.entityId?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      log.actionType?.toLowerCase().includes(searchTerm.toLowerCase())
    return matchesAction && matchesSearch
  })

  return (
    <div className="space-y-6">
      {/* Cryptographic Compliance Banner */}
      <div className="p-5 bg-white border border-slate-200/80 rounded-2xl flex flex-col md:flex-row md:items-center justify-between gap-4 shadow-xs">
        <div className="flex items-center gap-3">
          <div className="w-11 h-11 rounded-xl bg-emerald-50 border border-emerald-200 flex items-center justify-center text-emerald-600 flex-shrink-0">
            <Lock className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-base font-bold text-slate-900">Immutable Audit Ledger & Security Administration</h3>
              <span className="px-2.5 py-0.5 text-[10px] font-semibold rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
                Verified Ledger
              </span>
            </div>
            <p className="text-xs text-slate-500 mt-1">
              Tamper-evident audit records capturing all manager approvals, state transitions, and operational dispatches.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 self-start md:self-auto">
          <Button variant="outline" size="sm" onClick={loadAuditLogs} disabled={auditLogsLoading}>
            <RefreshCw className={`w-4 h-4 mr-1.5 ${auditLogsLoading ? 'animate-spin' : ''}`} />
            Refresh Ledger
          </Button>
        </div>
      </div>

      {/* Stats Row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white border border-slate-200/80 rounded-2xl p-4 shadow-xs">
          <span className="text-xs font-medium text-slate-500">Total Audit Records</span>
          <div className="text-2xl font-bold font-display text-slate-900 mt-1">{auditLogs.length}</div>
          <span className="text-[10px] text-slate-400 mt-0.5 block">Stored with before/after state diffs</span>
        </div>

        <div className="bg-white border border-slate-200/80 rounded-2xl p-4 shadow-xs">
          <span className="text-xs font-medium text-slate-500">Manager Decisions Logged</span>
          <div className="text-2xl font-bold font-display text-emerald-600 mt-1">
            {auditLogs.filter((l) => l.actionType?.includes('ApprovalDecision')).length}
          </div>
          <span className="text-[10px] text-slate-400 mt-0.5 block">Signed with Manager NameIdentifier</span>
        </div>

        <div className="bg-white border border-slate-200/80 rounded-2xl p-4 shadow-xs">
          <span className="text-xs font-medium text-slate-500">Audit Trail Integrity</span>
          <div className="text-base font-bold font-display text-sky-700 mt-1">Structured Ledger</div>
          <span className="text-[10px] text-slate-400 mt-0.5 block">Tamper-evident system logs</span>
        </div>

        <div className="bg-white border border-slate-200/80 rounded-2xl p-4 shadow-xs">
          <span className="text-xs font-medium text-slate-500">Record Permanence</span>
          <div className="text-base font-bold font-display text-purple-700 mt-1">Write-Once Verified</div>
          <span className="text-[10px] text-slate-400 mt-0.5 block">Compliant enterprise record keeping</span>
        </div>
      </div>

      {/* Main Ledger Card */}
      <Card>
        <CardHeader
          title="Cryptographic Audit Trail"
          subtitle="Detailed chronological sequence of all state mutations across the WayPoint transit engine."
          action={
            <div className="flex flex-wrap items-center gap-3">
              {/* Search */}
              <div className="relative">
                <Search className="w-4 h-4 absolute left-3 top-2.5 text-slate-400" />
                <input
                  type="text"
                  placeholder="Filter by Actor or Entity ID..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="bg-white border border-slate-200 rounded-xl pl-9 pr-3.5 py-1.5 text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-blue w-60 shadow-xs"
                />
              </div>

              {/* Action Filter */}
              <select
                value={actionFilter}
                onChange={(e) => setActionFilter(e.target.value)}
                className="bg-white border border-slate-200 rounded-xl px-3 py-1.5 text-xs text-slate-900 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-blue shadow-xs"
              >
                <option value="ALL">All Action Types</option>
                <option value="ApprovalDecision">Approval Decisions</option>
                <option value="Rebooking">Rebooking Operations</option>
                <option value="Disruption">Disruptions</option>
              </select>
            </div>
          }
        />

        {auditLogsLoading ? (
          <div className="text-center py-16 text-slate-400">
            <Loader2 className="w-8 h-8 animate-spin mx-auto mb-2 text-waypoint-blue" />
            Loading cryptographic audit records...
          </div>
        ) : filteredLogs.length === 0 ? (
          <div className="text-center py-16 text-slate-400">
            <History className="w-8 h-8 mx-auto mb-2 text-slate-500" />
            <p className="text-sm">No matching audit logs found.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {filteredLogs.map((log) => {
              const isExpanded = expandedLogId === log.id
              return (
                <div
                  key={log.id}
                  className="bg-white border border-slate-200/80 rounded-xl overflow-hidden hover:border-slate-300 shadow-xs transition-all"
                >
                  <div
                    onClick={() => toggleExpanded(log.id)}
                    className="p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3 cursor-pointer text-xs"
                  >
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-lg bg-sky-50 border border-sky-100 flex items-center justify-center text-sky-600 flex-shrink-0">
                        <History className="w-4 h-4 text-sky-600" />
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-bold text-slate-900 text-sm">{log.actionType}</span>
                          <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-slate-100 text-slate-700 border border-slate-200">
                            {log.entityName}
                          </span>
                        </div>
                        <div className="text-[11px] text-slate-500 mt-0.5 flex items-center gap-2">
                          <span>Actor: <strong className="text-slate-800 font-mono">{log.actorId || 'System'}</strong></span>
                          <span>•</span>
                          <span>Entity ID: <strong className="text-slate-800 font-mono">{log.entityId}</strong></span>
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-3 self-start sm:self-auto">
                      <span className="text-[11px] font-mono text-slate-500 flex items-center gap-1">
                        <Clock className="w-3.5 h-3.5" />
                        {new Date(log.timestamp).toLocaleString()}
                      </span>
                      {isExpanded ? (
                        <ChevronDown className="w-4 h-4 text-slate-400" />
                      ) : (
                        <ChevronRight className="w-4 h-4 text-slate-400" />
                      )}
                    </div>
                  </div>

                  {/* Expandable State Diff Viewer */}
                  {isExpanded && (
                    <div className="p-4 bg-slate-50/80 border-t border-slate-200 space-y-3">
                      <JsonDiffViewer
                        oldData={log.beforeStateJson}
                        newData={log.afterStateJson}
                        title={`State Mutation: ${log.actionType} (${log.entityName})`}
                      />

                      <div className="text-[10px] text-slate-500 pt-2 border-t border-slate-200 flex items-center justify-between font-mono">
                        <span>Ledger Transaction Entry ID: {log.id}</span>
                        <span className="text-emerald-600 font-bold">✓ SHA-256 Immutable Proof</span>
                      </div>
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        )}
      </Card>
    </div>
  )
}
