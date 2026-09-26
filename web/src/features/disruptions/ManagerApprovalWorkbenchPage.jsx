import React, { useState, useEffect, useCallback } from 'react'
import { disruptionApi } from './disruptionApi'
import { useDisruptionStore } from '@/store/disruptionStore'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import {
  CheckSquare, CheckCircle2, XCircle, AlertTriangle, ShieldCheck,
  RotateCcw, ArrowRight, Loader2, RefreshCw, MessageSquare, AlertCircle,
  Clock, DollarSign, Users, Bus, Zap
} from 'lucide-react'

export function ManagerApprovalWorkbenchPage() {
  const { pendingApprovals, approvalsLoading, setPendingApprovals, setApprovalsLoading } = useDisruptionStore()

  // Selected Proposal for Decision
  const [selectedProposal, setSelectedProposal] = useState(null)
  const [decisionType, setDecisionType] = useState('Approve') // Approve | Reject | RequestRevision
  const [comments, setComments] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [showDecisionModal, setShowDecisionModal] = useState(false)

  // Execution State
  const [executingProposalId, setExecutingProposalId] = useState(null)
  const [executionResult, setExecutionResult] = useState(null)

  // Toast
  const [actionMessage, setActionMessage] = useState(null)

  const showToast = (type, text) => {
    setActionMessage({ type, text })
    setTimeout(() => setActionMessage(null), 6000)
  }

  const fetchApprovals = useCallback(async () => {
    setApprovalsLoading(true)
    try {
      const data = await disruptionApi.getPendingApprovals()
      setPendingApprovals(data)
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to fetch pending approval queue.')
    } finally {
      setApprovalsLoading(false)
    }
  }, [setPendingApprovals, setApprovalsLoading])

  useEffect(() => {
    fetchApprovals()
  }, [fetchApprovals])

  const handleOpenDecisionModal = (proposal, type) => {
    setSelectedProposal(proposal)
    setDecisionType(type)
    setComments(
      type === 'Approve'
        ? 'Approved — Replacement service has sufficient capacity. Fare protection guaranteed.'
        : type === 'Reject'
        ? 'Rejected — Alternative schedule conflicts with existing corridor flow.'
        : 'Revision Requested — Please check secondary express corridor availability.'
    )
    setShowDecisionModal(true)
  }

  const handleSubmitDecision = async () => {
    if (!comments.trim()) {
      showToast('error', 'Manager decision comments are required for audit trail compliance (BR-APPROVAL-002).')
      return
    }

    setIsSubmitting(true)
    try {
      await disruptionApi.submitApprovalDecision(selectedProposal.rebookingProposalId, {
        decision: decisionType,
        comments: comments.trim(),
      })
      showToast('success', `Decision '${decisionType}' submitted successfully. Audit log created.`)
      setShowDecisionModal(false)
      setSelectedProposal(null)
      setComments('')
      fetchApprovals()
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to submit manager approval decision.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleExecuteRebooking = async (proposalId) => {
    setExecutingProposalId(proposalId)
    setExecutionResult(null)
    try {
      const result = await disruptionApi.executeRebooking(proposalId)
      setExecutionResult(result)
      showToast('success', `Rebooking executed atomically! Transferred ${result.transferredBookingsCount} passengers to replacement service.`)
      fetchApprovals()
    } catch (err) {
      showToast('error', err.response?.data?.detail || 'Failed to execute rebooking transaction.')
    } finally {
      setExecutingProposalId(null)
    }
  }

  return (
    <div className="space-y-6">
      {/* Toast Alert */}
      {actionMessage && (
        <div
          className={`flex items-center gap-2 px-4 py-3 rounded-xl text-sm border animate-in fade-in slide-in-from-top-2 ${
            actionMessage.type === 'success'
              ? 'bg-emerald-950/70 border-emerald-700/60 text-emerald-200'
              : 'bg-rose-950/70 border-rose-700/60 text-rose-200'
          }`}
        >
          {actionMessage.type === 'success' ? <CheckCircle2 className="w-5 h-5 flex-shrink-0" /> : <AlertCircle className="w-5 h-5 flex-shrink-0" />}
          <span className="font-medium">{actionMessage.text}</span>
        </div>
      )}

      {/* Execution Result Banner */}
      {executionResult && (
        <div className="p-5 bg-emerald-950/40 border border-emerald-500/40 rounded-2xl shadow-lg space-y-3 animate-in fade-in">
          <div className="flex items-center justify-between border-b border-emerald-500/20 pb-2">
            <div className="flex items-center gap-2">
              <Zap className="w-5 h-5 text-emerald-400" />
              <h4 className="font-bold text-white text-base">Transactional Rebooking Executed (BR-REBOOK-001)</h4>
            </div>
            <span className="text-xs font-mono text-emerald-300">Transaction ID: {executionResult.transactionId || 'Committed'}</span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
            <div className="bg-slate-950/70 p-3 rounded-xl border border-slate-800">
              <span className="text-slate-400 block">Passengers Transferred</span>
              <span className="text-lg font-bold text-white">{executionResult.transferredBookingsCount} pax</span>
            </div>
            <div className="bg-slate-950/70 p-3 rounded-xl border border-slate-800">
              <span className="text-slate-400 block">Old Seats Released</span>
              <span className="text-lg font-bold text-amber-300">{executionResult.releasedSeatsCount} seats</span>
            </div>
            <div className="bg-slate-950/70 p-3 rounded-xl border border-slate-800">
              <span className="text-slate-400 block">New Seats Locked</span>
              <span className="text-lg font-bold text-sky-300">{executionResult.lockedSeatsCount} seats</span>
            </div>
            <div className="bg-slate-950/70 p-3 rounded-xl border border-slate-800">
              <span className="text-slate-400 block">Tickets Re-Issued</span>
              <span className="text-lg font-bold text-emerald-300">{executionResult.reissuedTicketsCount} tickets</span>
            </div>
          </div>

          <div className="text-xs text-slate-300">
            Fare Protection Guarantee (<strong>BR-REBOOK-002</strong>): Total fare refund issued:{' '}
            <strong className="text-emerald-300">LKR {executionResult.totalFareDifferenceRefunded?.toLocaleString() || '0'}</strong>.
            All passengers notified via automated ServiceAlert broadcast.
          </div>
        </div>
      )}

      {/* Policy Governance Banner */}
      <div className="p-4 bg-slate-900 border border-slate-800 rounded-2xl flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-400 flex-shrink-0">
            <ShieldCheck className="w-6 h-6" />
          </div>
          <div>
            <h4 className="text-sm font-bold text-white">Manager Approval Boundary (BR-APPROVAL-001)</h4>
            <p className="text-xs text-slate-400">
              High-impact operational proposals require authenticated Transport Manager sign-off before seat reallocation or ticket re-issuance.
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={fetchApprovals} disabled={approvalsLoading}>
            <RefreshCw className={`w-4 h-4 mr-1.5 ${approvalsLoading ? 'animate-spin' : ''}`} />
            Refresh Queue
          </Button>
        </div>
      </div>

      {/* Pending Approval Cards */}
      <Card>
        <CardHeader
          title="Pending Approval Queue"
          subtitle={`Showing ${pendingApprovals.length} proposal(s) awaiting managerial review.`}
        />

        {approvalsLoading ? (
          <div className="text-center py-12 text-slate-400">
            <Loader2 className="w-6 h-6 animate-spin mx-auto mb-2 text-waypoint-blue" />
            Loading pending managerial approvals...
          </div>
        ) : pendingApprovals.length === 0 ? (
          <div className="text-center py-12 text-slate-400">
            <CheckCircle2 className="w-10 h-10 mx-auto mb-2 text-emerald-400/60" />
            <h4 className="font-semibold text-white">All Clear!</h4>
            <p className="text-xs text-slate-400 mt-1">No pending rebooking proposals require manager approval at this time.</p>
          </div>
        ) : (
          <div className="space-y-4">
            {pendingApprovals.map((p) => (
              <div
                key={p.rebookingProposalId}
                className="bg-slate-950 border border-slate-800 rounded-2xl p-5 hover:border-slate-700 transition-all space-y-4"
              >
                {/* Proposal Header */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800 pb-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-xs text-sky-400">Proposal #{p.rebookingProposalId.substring(0, 8)}</span>
                      <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                        PendingManagerApproval
                      </span>
                    </div>
                    <div className="text-xs text-slate-400 mt-1">
                      Originating Disruption Case: <span className="font-mono text-slate-300">{p.disruptionCaseId}</span>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      variant="success"
                      size="sm"
                      onClick={() => handleOpenDecisionModal(p, 'Approve')}
                    >
                      <CheckCircle2 className="w-4 h-4 mr-1" />
                      Approve
                    </Button>
                    <Button
                      variant="danger"
                      size="sm"
                      onClick={() => handleOpenDecisionModal(p, 'Reject')}
                    >
                      <XCircle className="w-4 h-4 mr-1" />
                      Reject
                    </Button>
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => handleOpenDecisionModal(p, 'RequestRevision')}
                    >
                      <RotateCcw className="w-4 h-4 mr-1" />
                      Revise
                    </Button>
                  </div>
                </div>

                {/* Service Comparison Matrix */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {/* Disrupted Service */}
                  <div className="bg-slate-900/80 border border-rose-950/60 rounded-xl p-4 space-y-2">
                    <div className="flex items-center justify-between text-xs text-rose-400 font-semibold uppercase">
                      <span>Disrupted Service</span>
                      <TransitBadge status="Disrupted" label="Halted" />
                    </div>
                    <div className="text-lg font-bold font-display text-white flex items-center gap-2">
                      <Bus className="w-5 h-5 text-rose-400" />
                      {p.originalServiceCode}
                    </div>
                    <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t border-slate-800">
                      <div>
                        <span className="text-slate-400 block">Affected Bookings</span>
                        <span className="font-semibold text-white">{p.affectedPassengersCount} passengers</span>
                      </div>
                      <div>
                        <span className="text-slate-400 block">Revenue at Risk</span>
                        <span className="font-semibold text-amber-400">LKR {p.revenueAtRisk?.toLocaleString()}</span>
                      </div>
                    </div>
                  </div>

                  {/* Replacement Service */}
                  <div className="bg-slate-900/80 border border-emerald-950/60 rounded-xl p-4 space-y-2">
                    <div className="flex items-center justify-between text-xs text-emerald-400 font-semibold uppercase">
                      <span>Replacement Assignment</span>
                      <TransitBadge status="Available" label="Scheduled" />
                    </div>
                    <div className="text-lg font-bold font-display text-white flex items-center gap-2">
                      <Bus className="w-5 h-5 text-emerald-400" />
                      {p.replacementServiceCode}
                    </div>
                    <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t border-slate-800">
                      <div>
                        <span className="text-slate-400 block">Fare Protection</span>
                        <span className="font-semibold text-emerald-300">Guaranteed (BR-REBOOK-002)</span>
                      </div>
                      <div>
                        <span className="text-slate-400 block">Timetable Match</span>
                        <span className="font-semibold text-sky-300">Optimal Corridor</span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Direct Transaction Execution (If Manager Wishes to Execute Directly) */}
                <div className="flex items-center justify-between pt-2 border-t border-slate-800/80 text-xs">
                  <span className="text-slate-400">
                    Authority: Requires <strong className="text-slate-200">TransportManager</strong> or <strong className="text-slate-200">Admin</strong> role to submit decisions.
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      {/* Decision Submission Modal */}
      {showDecisionModal && selectedProposal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-in fade-in">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-lg w-full p-6 shadow-2xl space-y-5">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div className="flex items-center gap-2">
                <CheckSquare className="w-5 h-5 text-waypoint-blue" />
                <h3 className="text-lg font-bold font-display text-white">Manager Operational Decision</h3>
              </div>
              <button onClick={() => setShowDecisionModal(false)} className="text-slate-400 hover:text-white">
                <XCircle className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              <div className="p-3 bg-slate-950 rounded-xl border border-slate-800 text-xs space-y-1">
                <div className="text-slate-400">Proposal Under Review</div>
                <div className="text-sm font-semibold text-white">
                  Transfer <span className="text-amber-400">{selectedProposal.originalServiceCode}</span> &rarr;{' '}
                  <span className="text-emerald-400">{selectedProposal.replacementServiceCode}</span>
                </div>
                <div className="text-slate-400">Impacted Passengers: {selectedProposal.affectedPassengersCount}</div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1.5">
                  Selected Action
                </label>
                <div className="grid grid-cols-3 gap-2">
                  {['Approve', 'Reject', 'RequestRevision'].map((action) => (
                    <button
                      type="button"
                      key={action}
                      onClick={() => setDecisionType(action)}
                      className={`py-2 px-3 text-xs font-semibold rounded-lg border text-center transition-all ${
                        decisionType === action
                          ? action === 'Approve'
                            ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30 ring-1 ring-emerald-400'
                            : action === 'Reject'
                            ? 'bg-rose-500/20 text-rose-300 border-rose-500/30 ring-1 ring-rose-400'
                            : 'bg-amber-500/20 text-amber-300 border-amber-500/30 ring-1 ring-amber-400'
                          : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                      }`}
                    >
                      {action === 'RequestRevision' ? 'Revise' : action}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1.5">
                  Manager Rationale & Audit Reasoning *
                </label>
                <textarea
                  value={comments}
                  onChange={(e) => setComments(e.target.value)}
                  placeholder="State the operational justification for this decision (recorded immutably in AuditLog)..."
                  rows="3"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-sm text-white focus:outline-none focus:border-waypoint-blue"
                  required
                />
                <span className="text-[11px] text-slate-500 mt-1 block">
                  Mandatory under rule <strong>BR-APPROVAL-002</strong>. Logged with Manager ID and timestamp.
                </span>
              </div>
            </div>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-800">
              <Button variant="outline" size="sm" onClick={() => setShowDecisionModal(false)}>
                Cancel
              </Button>
              <Button
                variant={decisionType === 'Approve' ? 'success' : decisionType === 'Reject' ? 'danger' : 'secondary'}
                size="sm"
                onClick={handleSubmitDecision}
                disabled={isSubmitting}
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin mr-1.5" />
                    Submitting...
                  </>
                ) : (
                  `Confirm ${decisionType}`
                )}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
