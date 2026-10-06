import React, { useState, useEffect, useCallback } from 'react'
import { disruptionApi } from './disruptionApi'
import { useDisruptionStore } from '@/store/disruptionStore'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import {
  AlertTriangle, Flame, ShieldAlert, Users, DollarSign,
  Plus, CheckCircle2, AlertCircle, Loader2, ArrowRight,
  ChevronRight, RefreshCw, X, FileText, Bus
} from 'lucide-react'

const SEVERITY_COLORS = {
  Critical: 'bg-rose-50 text-rose-700 border-rose-200',
  Major: 'bg-orange-50 text-orange-700 border-orange-200',
  Minor: 'bg-amber-50 text-amber-700 border-amber-200',
}

const STATUS_BADGES = {
  Logged: 'Held',
  Analyzing: 'Delayed',
  ProposalGenerated: 'Luxury',
  PendingApproval: 'Held',
  Approved: 'Available',
  Executing: 'Express',
  Resolved: 'Available',
  Cancelled: 'Disrupted',
}

export function DisruptionIntakePage() {
  const {
    disruptions,
    disruptionsLoading,
    setDisruptions,
    setDisruptionsLoading,
    services,
    setServices,
  } = useDisruptionStore()

  // Form State
  const [selectedServiceId, setSelectedServiceId] = useState('')
  const [severity, setSeverity] = useState('Major')
  const [reason, setReason] = useState('')
  const [affectedPassengerOverride, setAffectedPassengerOverride] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Modals & Panels
  const [showLogModal, setShowLogModal] = useState(false)
  const [selectedImpact, setSelectedImpact] = useState(null)
  const [impactLoading, setImpactLoading] = useState(false)
  const [showProposalModal, setShowProposalModal] = useState(false)
  const [targetDisruption, setTargetDisruption] = useState(null)
  const [replacementServiceId, setReplacementServiceId] = useState('')
  const [proposalSubmitting, setProposalSubmitting] = useState(false)

  // Notification Toast
  const [actionMessage, setActionMessage] = useState(null)

  const showToast = (type, text) => {
    setActionMessage({ type, text })
    setTimeout(() => setActionMessage(null), 5000)
  }

  const loadData = useCallback(async () => {
    setDisruptionsLoading(true)
    try {
      const [disruptionsRes, servicesRes] = await Promise.all([
        disruptionApi.getDisruptions({ pageNumber: 1, pageSize: 50 }),
        disruptionApi.getServices().catch(() => []),
      ])
      setDisruptions(disruptionsRes)
      if (servicesRes) setServices(servicesRes)
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to load disruptions')
    } finally {
      setDisruptionsLoading(false)
    }
  }, [setDisruptions, setDisruptionsLoading, setServices])

  useEffect(() => {
    loadData()
  }, [loadData])

  const handleLogDisruption = async (e) => {
    e.preventDefault()
    if (!selectedServiceId) {
      showToast('error', 'Please select a disrupted transit service.')
      return
    }
    if (!reason.trim()) {
      showToast('error', 'Please enter a valid disruption reason.')
      return
    }

    setIsSubmitting(true)
    try {
      const payload = {
        disruptedServiceId: selectedServiceId,
        reason: reason.trim(),
        severity: severity,
        affectedPassengerOverride: affectedPassengerOverride ? parseInt(affectedPassengerOverride, 10) : null,
      }
      await disruptionApi.logDisruption(payload)
      showToast('success', 'Disruption incident logged successfully! Service marked Disrupted.')
      setShowLogModal(false)
      setReason('')
      setAffectedPassengerOverride('')
      setSelectedServiceId('')
      loadData()
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to log disruption')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleViewImpact = async (disruption) => {
    setImpactLoading(true)
    setSelectedImpact(null)
    try {
      const impact = await disruptionApi.getDisruptionImpact(disruption.id)
      setSelectedImpact({ ...impact, disruption })
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to calculate passenger impact')
    } finally {
      setImpactLoading(false)
    }
  }

  const handleOpenProposalModal = (disruption) => {
    setTargetDisruption(disruption)
    setReplacementServiceId('')
    setShowProposalModal(true)
  }

  const handleGenerateProposal = async () => {
    if (!targetDisruption || !replacementServiceId) {
      showToast('error', 'Please select a replacement service.')
      return
    }

    setProposalSubmitting(true)
    try {
      await disruptionApi.generateRebookingProposal({
        disruptionCaseId: targetDisruption.id,
        replacementServiceId: replacementServiceId,
      })
      showToast('success', 'Rebooking proposal created successfully and routed to Manager Approval queue!')
      setShowProposalModal(false)
      setTargetDisruption(null)
      loadData()
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to create rebooking proposal')
    } finally {
      setProposalSubmitting(false)
    }
  }

  // Quick stats
  const totalDisruptions = disruptions.length
  const criticalCount = disruptions.filter((d) => d.severity === 'Critical').length
  const totalAffected = disruptions.reduce((acc, d) => acc + (d.affectedPassengerCount || 0), 0)

  return (
    <div className="space-y-6">
      {/* Toast Alert */}
      {actionMessage && (
        <div
          className={`flex items-center gap-2 px-4 py-3 rounded-xl text-sm border animate-in fade-in slide-in-from-top-2 ${
            actionMessage.type === 'success'
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-rose-50 border-rose-200 text-rose-800'
          }`}
        >
          {actionMessage.type === 'success' ? <CheckCircle2 className="w-5 h-5 flex-shrink-0 text-emerald-600" /> : <AlertCircle className="w-5 h-5 flex-shrink-0 text-rose-600" />}
          <span className="font-medium">{actionMessage.text}</span>
        </div>
      )}

      {/* Metrics Row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-500">Active Incidents</span>
            <AlertTriangle className="w-5 h-5 text-amber-500" />
          </div>
          <div className="text-2xl font-bold font-display text-slate-900 mt-2">{totalDisruptions}</div>
          <span className="text-[11px] text-slate-500 mt-1 block">Live operational cases</span>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-500">Critical Disruptions</span>
            <Flame className="w-5 h-5 text-rose-500" />
          </div>
          <div className="text-2xl font-bold font-display text-rose-600 mt-2">{criticalCount}</div>
          <span className="text-[11px] text-slate-500 mt-1 block">High impact cancellation / halt</span>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-500">Impacted Passengers</span>
            <Users className="w-5 h-5 text-sky-500" />
          </div>
          <div className="text-2xl font-bold font-display text-slate-900 mt-2">{totalAffected}</div>
          <span className="text-[11px] text-slate-500 mt-1 block">Requiring rebooking or refunds</span>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-500">Approval Policy Gate</span>
            <ShieldAlert className="w-5 h-5 text-emerald-500" />
          </div>
          <div className="text-base font-bold font-display text-emerald-700 mt-2">BR-APPROVAL-001</div>
          <span className="text-[11px] text-slate-500 mt-1 block">&gt;15m shift requires Manager role</span>
        </div>
      </div>

      {/* Main Content Area */}
      <Card>
        <CardHeader
          title="Transit Disruption Incident Intake"
          subtitle="Real-time incident registration, deterministic impact assessment, and automated proposal initiation."
          action={
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={loadData} disabled={disruptionsLoading}>
                <RefreshCw className={`w-4 h-4 mr-1.5 ${disruptionsLoading ? 'animate-spin' : ''}`} />
                Refresh
              </Button>
              <Button variant="primary" size="sm" onClick={() => setShowLogModal(true)}>
                <Plus className="w-4 h-4 mr-1.5" />
                Log Disruption
              </Button>
            </div>
          }
        />

        {/* Table of Active Disruptions */}
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50/50 text-slate-500 text-xs font-semibold uppercase tracking-wider border-b border-slate-200">
              <tr>
                <th className="py-3 px-4">Service</th>
                <th className="py-3 px-4">Severity</th>
                <th className="py-3 px-4">Reason</th>
                <th className="py-3 px-4 text-center">Affected</th>
                <th className="py-3 px-4">Status</th>
                <th className="py-3 px-4">Logged At</th>
                <th className="py-3 px-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {disruptionsLoading && disruptions.length === 0 ? (
                <tr>
                  <td colSpan="7" className="text-center py-12 text-slate-500">
                    <Loader2 className="w-6 h-6 animate-spin mx-auto mb-2 text-waypoint-primary" />
                    Loading disruption incidents...
                  </td>
                </tr>
              ) : disruptions.length === 0 ? (
                <tr>
                  <td colSpan="7" className="text-center py-12 text-slate-500">
                    No active disruptions recorded in the transit network.
                  </td>
                </tr>
              ) : (
                disruptions.map((d) => (
                  <tr key={d.id} className="hover:bg-slate-50/80 transition-colors">
                    <td className="py-3.5 px-4 font-mono font-medium text-slate-900">
                      <div className="flex items-center gap-2">
                        <Bus className="w-4 h-4 text-slate-400" />
                        <span>{d.disruptedServiceCode || 'Service'}</span>
                      </div>
                    </td>
                    <td className="py-3.5 px-4">
                      <span className={`px-2.5 py-1 text-xs font-semibold rounded-full border ${SEVERITY_COLORS[d.severity] || SEVERITY_COLORS.Minor}`}>
                        {d.severity}
                      </span>
                    </td>
                    <td className="py-3.5 px-4 max-w-xs truncate text-slate-600 font-sans" title={d.reason}>
                      {d.reason}
                    </td>
                    <td className="py-3.5 px-4 text-center font-medium text-slate-900">
                      <span className="px-2 py-0.5 rounded bg-slate-100 text-sky-700 border border-slate-200 text-xs">
                        {d.affectedPassengerCount} pax
                      </span>
                    </td>
                    <td className="py-3.5 px-4">
                      <TransitBadge status={STATUS_BADGES[d.status] || 'Held'} label={d.status} />
                    </td>
                    <td className="py-3.5 px-4 text-xs text-slate-500 whitespace-nowrap">
                      {new Date(d.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </td>
                    <td className="py-3.5 px-4 text-right space-x-2 whitespace-nowrap">
                      <Button variant="outline" size="sm" onClick={() => handleViewImpact(d)}>
                        <FileText className="w-3.5 h-3.5 mr-1" />
                        Impact
                      </Button>
                      <Button variant="secondary" size="sm" onClick={() => handleOpenProposalModal(d)}>
                        <ArrowRight className="w-3.5 h-3.5 mr-1" />
                        Rebook
                      </Button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Log Disruption Modal */}
      {showLogModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in">
          <div className="bg-white border border-slate-200 rounded-2xl max-w-lg w-full p-6 shadow-2xl space-y-5">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div className="flex items-center gap-2">
                <AlertTriangle className="w-5 h-5 text-rose-500" />
                <h3 className="text-lg font-bold font-display text-slate-900">Log Disruption Incident</h3>
              </div>
              <button onClick={() => setShowLogModal(false)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleLogDisruption} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-slate-700 mb-1.5">
                  Select Disrupted Service *
                </label>
                <select
                  value={selectedServiceId}
                  onChange={(e) => setSelectedServiceId(e.target.value)}
                  className="w-full bg-slate-50 border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm text-slate-900 focus:outline-none focus:border-waypoint-primary"
                  required
                >
                  <option value="">-- Choose scheduled transit service --</option>
                  {services.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.serviceCode} — {s.routeName || 'Corridor'} (Bus: {s.busPlate || 'TBD'})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-700 mb-1.5">
                  Severity Classification *
                </label>
                <div className="grid grid-cols-3 gap-2">
                  {['Minor', 'Major', 'Critical'].map((level) => (
                    <button
                      type="button"
                      key={level}
                      onClick={() => setSeverity(level)}
                      className={`py-2 px-3 text-xs font-semibold rounded-lg border text-center transition-all ${
                        severity === level
                          ? `${SEVERITY_COLORS[level]} ring-1 ring-slate-400/20 shadow-sm font-bold`
                          : 'bg-slate-50 border-slate-200 text-slate-600 hover:text-slate-900'
                      }`}
                    >
                      {level}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-700 mb-1.5">
                  Disruption Root Cause / Reason *
                </label>
                <textarea
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder="e.g. Engine overheat on Southern Expressway near Kurundugahahetekma, bus halted."
                  rows="3"
                  className="w-full bg-slate-50 border border-slate-200 rounded-xl p-3 text-sm text-slate-900 focus:outline-none focus:border-waypoint-primary"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-700 mb-1.5">
                  Affected Passenger Override (Optional)
                </label>
                <input
                  type="number"
                  min="0"
                  value={affectedPassengerOverride}
                  onChange={(e) => setAffectedPassengerOverride(e.target.value)}
                  placeholder="Auto-calculated from confirmed bookings if blank"
                  className="w-full bg-slate-50 border border-slate-200 rounded-xl px-3.5 py-2 text-sm text-slate-900 focus:outline-none focus:border-waypoint-primary"
                />
                <span className="text-[11px] text-slate-500 mt-1 block">
                  Leave blank to auto-query actual confirmed bookings from database.
                </span>
              </div>

              <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
                <Button type="button" variant="outline" size="sm" onClick={() => setShowLogModal(false)}>
                  Cancel
                </Button>
                <Button type="submit" variant="danger" size="sm" disabled={isSubmitting}>
                  {isSubmitting ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin mr-1.5" />
                      Logging Incident...
                    </>
                  ) : (
                    'Log Incident & Halt Service'
                  )}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Impact Assessment Modal */}
      {selectedImpact && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in">
          <div className="bg-white border border-slate-200 rounded-2xl max-w-lg w-full p-6 shadow-2xl space-y-5">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div className="flex items-center gap-2">
                <FileText className="w-5 h-5 text-waypoint-primary" />
                <h3 className="text-lg font-bold font-display text-slate-900">Deterministic Impact Analysis</h3>
              </div>
              <button onClick={() => setSelectedImpact(null)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 space-y-1">
                <div className="text-xs text-slate-500 font-mono">Disruption Reference</div>
                <div className="text-sm font-semibold text-slate-900">
                  Case ID: <span className="font-mono text-sky-600">{selectedImpact.disruptionCaseId}</span>
                </div>
                <div className="text-xs text-slate-600 mt-1">{selectedImpact.disruption?.reason}</div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="p-3 bg-slate-50 rounded-xl border border-slate-200">
                  <span className="text-[11px] text-slate-500 block">Affected Bookings</span>
                  <span className="text-xl font-bold font-display text-slate-900">
                    {selectedImpact.affectedPassengerCount} passengers
                  </span>
                </div>

                <div className="p-3 bg-slate-50 rounded-xl border border-slate-200">
                  <span className="text-[11px] text-slate-500 block">Revenue At Risk</span>
                  <span className="text-xl font-bold font-display text-amber-600">
                    LKR {selectedImpact.revenueAtRisk?.toLocaleString()}
                  </span>
                </div>
              </div>

              <div className="p-3.5 rounded-xl border bg-slate-50 border-slate-200">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold text-slate-700">Approval Boundary</span>
                  <span className={`text-xs px-2 py-0.5 rounded font-semibold ${
                    selectedImpact.requiresManagerApproval
                      ? 'bg-rose-50 text-rose-700 border border-rose-200'
                      : 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                  }`}>
                    {selectedImpact.requiresManagerApproval ? 'Manager Approval Mandatory' : 'Auto-Exec Allowed'}
                  </span>
                </div>
                <p className="text-xs text-slate-500 mt-1">
                  Enforces Rule <strong>BR-APPROVAL-001</strong>. High-impact operational changes or cancellations require Transport Manager sign-off.
                </p>
              </div>

              {selectedImpact.affectedPassengerEmails?.length > 0 && (
                <div>
                  <span className="text-xs font-semibold uppercase text-slate-700 mb-1 block">
                    Notified Passenger Contacts ({selectedImpact.affectedPassengerEmails.length})
                  </span>
                  <div className="max-h-24 overflow-y-auto bg-slate-50 border border-slate-200 rounded-lg p-2 text-xs font-mono text-slate-700 space-y-1">
                    {selectedImpact.affectedPassengerEmails.map((email, idx) => (
                      <div key={idx}>{email}</div>
                    ))}
                  </div>
                </div>
              )}
            </div>

            <div className="flex justify-end pt-3 border-t border-slate-100">
              <Button variant="primary" size="sm" onClick={() => setSelectedImpact(null)}>
                Close Analysis
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Generate Rebooking Proposal Modal */}
      {showProposalModal && targetDisruption && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in">
          <div className="bg-white border border-slate-200 rounded-2xl max-w-lg w-full p-6 shadow-2xl space-y-5">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div className="flex items-center gap-2">
                <Bus className="w-5 h-5 text-waypoint-primary" />
                <h3 className="text-lg font-bold font-display text-slate-900">Generate Rebooking Proposal</h3>
              </div>
              <button onClick={() => setShowProposalModal(false)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              <div className="p-3 bg-slate-50 rounded-xl border border-slate-200 text-xs space-y-1">
                <div className="text-slate-500">Target Disruption</div>
                <div className="text-sm font-semibold text-slate-900">{targetDisruption.disruptedServiceCode}</div>
                <div className="text-slate-600">{targetDisruption.reason}</div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-700 mb-1.5">
                  Select Replacement Service *
                </label>
                <select
                  value={replacementServiceId}
                  onChange={(e) => setReplacementServiceId(e.target.value)}
                  className="w-full bg-slate-50 border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm text-slate-900 focus:outline-none focus:border-waypoint-primary"
                  required
                >
                  <option value="">-- Choose available alternative transit service --</option>
                  {services
                    .filter((s) => s.id !== targetDisruption.disruptedServiceId)
                    .map((s) => (
                      <option key={s.id} value={s.id}>
                        {s.serviceCode} — {s.routeName} (Fare: LKR {s.baseFare})
                      </option>
                    ))}
                </select>
                <span className="text-[11px] text-slate-500 mt-1 block">
                  Fare protection rule <strong>BR-REBOOK-002</strong> will guarantee passengers will not be charged extra.
                </span>
              </div>
            </div>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
              <Button variant="outline" size="sm" onClick={() => setShowProposalModal(false)}>
                Cancel
              </Button>
              <Button variant="secondary" size="sm" onClick={handleGenerateProposal} disabled={proposalSubmitting}>
                {proposalSubmitting ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin mr-1.5" />
                    Generating...
                  </>
                ) : (
                  'Create Rebooking Proposal'
                )}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
