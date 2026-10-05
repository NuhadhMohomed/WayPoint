import React, { useState, useEffect } from 'react'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'
import { adminApi } from '../adminApi'
import { Shield, Bus, Scale, User, AlertCircle, Loader2 } from 'lucide-react'

const ROLE_OPTIONS = [
  {
    role: 'Passenger',
    title: 'Passenger (Commuter)',
    desc: 'Public traveler: Search journeys, place seat holds, pay, download tickets, and review completed trips.',
    icon: User,
    color: 'emerald'
  },
  {
    role: 'Operator',
    title: 'Transit Operator / Dispatcher',
    desc: 'Depot & Fleet staff: Manage buses, seat designs, driver rostering, boarding manifests, and log incidents.',
    icon: Bus,
    color: 'blue'
  },
  {
    role: 'TransportManager',
    title: 'Transport Manager (Regulatory Authority)',
    desc: 'Decision-maker: Evaluates AI mitigation plans, signs off on high-impact cancellations and executes mass rebooking.',
    icon: Scale,
    color: 'amber'
  },
  {
    role: 'Admin',
    title: 'System Administrator',
    desc: 'Super-user: Platform governance, user and role lifecycle management, security auditing, and system configuration.',
    icon: Shield,
    color: 'purple'
  }
]

export function ChangeRoleModal({ isOpen, onClose, targetUser, currentUser, onRoleUpdated }) {
  const [selectedRole, setSelectedRole] = useState('')
  const [reason, setReason] = useState('')
  const [operatorCode, setOperatorCode] = useState('')
  const [companyName, setCompanyName] = useState('')
  const [assignedRegion, setAssignedRegion] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState(null)

  useEffect(() => {
    if (targetUser) {
      setSelectedRole(targetUser.role || 'Passenger')
      setReason('')
      setErrorMessage(null)
    }
  }, [targetUser])

  if (!targetUser) return null

  const isSelf = currentUser && (currentUser.id === targetUser.id || currentUser.email === targetUser.email)
  const isDemotingSelf = isSelf && targetUser.role === 'Admin' && selectedRole !== 'Admin'

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!reason.trim()) {
      setErrorMessage('A valid administrative reason is required for security audit compliance.')
      return
    }

    if (isDemotingSelf) {
      setErrorMessage('Administrators cannot demote their own account (BR-ADMIN-002).')
      return
    }

    setIsSubmitting(true)
    setErrorMessage(null)

    try {
      const payload = {
        role: selectedRole,
        reason: reason.trim(),
        operatorCode: selectedRole === 'Operator' ? operatorCode.trim() : undefined,
        companyName: selectedRole === 'Operator' ? companyName.trim() : undefined,
        assignedRegion: selectedRole === 'Operator' ? assignedRegion.trim() : undefined
      }

      const updated = await adminApi.updateUserRole(targetUser.id, payload)
      onRoleUpdated(updated)
      onClose()
    } catch (err) {
      setErrorMessage(err.response?.data?.detail || err.response?.data?.title || 'Failed to update user role.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Reassign System Role"
      subtitle={`Modify permissions and operational authority for ${targetUser.fullName || targetUser.email}`}
      maxWidth="max-w-xl"
    >
      <form onSubmit={handleSubmit} className="p-6 space-y-5">
        {/* User Card */}
        <div className="p-3.5 bg-slate-900 border border-slate-800 rounded-xl flex items-center justify-between">
          <div>
            <div className="text-sm font-bold text-white">{targetUser.fullName}</div>
            <div className="text-xs text-slate-400">{targetUser.email}</div>
          </div>
          <span className="px-2.5 py-1 text-xs font-semibold rounded-lg bg-slate-800 text-slate-300 border border-slate-700">
            Current: {targetUser.role}
          </span>
        </div>

        {/* Self-Demotion Alert */}
        {isSelf && targetUser.role === 'Admin' && (
          <div className="p-3 bg-amber-500/10 border border-amber-500/20 rounded-xl flex items-start gap-2.5 text-xs text-amber-300">
            <AlertCircle className="w-4 h-4 flex-shrink-0 mt-0.5 text-amber-400" />
            <div>
              <strong>Self-Demotion Lockout Guard (BR-ADMIN-002):</strong> You cannot demote your own administrator account. Another active administrator must make this change.
            </div>
          </div>
        )}

        {/* Error message */}
        {errorMessage && (
          <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-xl text-xs text-red-400">
            {errorMessage}
          </div>
        )}

        {/* Role Radios */}
        <div className="space-y-2.5">
          <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider">
            Select New Authority Tier
          </label>
          <div className="grid grid-cols-1 gap-2">
            {ROLE_OPTIONS.map((opt) => {
              const Icon = opt.icon
              const isSelected = selectedRole === opt.role
              const disabled = isSelf && targetUser.role === 'Admin' && opt.role !== 'Admin'

              return (
                <label
                  key={opt.role}
                  className={`flex items-start gap-3 p-3 rounded-xl border cursor-pointer transition-all ${
                    disabled
                      ? 'opacity-40 cursor-not-allowed bg-slate-950 border-slate-900'
                      : isSelected
                      ? 'bg-waypoint-primary/10 border-waypoint-primary text-white shadow-sm'
                      : 'bg-slate-900/60 border-slate-800 text-slate-300 hover:bg-slate-900 hover:border-slate-700'
                  }`}
                >
                  <input
                    type="radio"
                    name="roleOption"
                    value={opt.role}
                    checked={isSelected}
                    disabled={disabled}
                    onChange={(e) => setSelectedRole(e.target.value)}
                    className="mt-1 text-waypoint-primary focus:ring-waypoint-primary"
                  />
                  <div className="flex-1">
                    <div className="flex items-center gap-1.5 text-xs font-bold text-white">
                      <Icon className="w-3.5 h-3.5 text-waypoint-primary" />
                      {opt.title}
                    </div>
                    <p className="text-[11px] text-slate-400 mt-0.5 leading-relaxed">
                      {opt.desc}
                    </p>
                  </div>
                </label>
              )
            })}
          </div>
        </div>

        {/* Operator Profile Inputs (if transitioning to Operator) */}
        {selectedRole === 'Operator' && targetUser.role !== 'Operator' && (
          <div className="p-4 bg-slate-900 border border-slate-800 rounded-xl space-y-3">
            <span className="text-xs font-bold text-slate-200 block">
              Operator Depot Profile Configuration (BR-ADMIN-003)
            </span>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="block text-[11px] font-medium text-slate-400 mb-1">Operator Code</label>
                <input
                  type="text"
                  placeholder="e.g. OP-COLOMBO-01"
                  value={operatorCode}
                  onChange={(e) => setOperatorCode(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white focus:outline-none focus:border-waypoint-primary"
                />
              </div>
              <div>
                <label className="block text-[11px] font-medium text-slate-400 mb-1">Company / Consortium</label>
                <input
                  type="text"
                  placeholder="e.g. SLTB Western Region"
                  value={companyName}
                  onChange={(e) => setCompanyName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white focus:outline-none focus:border-waypoint-primary"
                />
              </div>
            </div>
          </div>
        )}

        {/* Reason for Audit */}
        <div>
          <label className="block text-xs font-semibold text-slate-300 mb-1">
            Reason for Administrative Change <span className="text-red-400">*</span>
          </label>
          <textarea
            rows={2}
            required
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Document regulatory or organizational reason (recorded in immutable audit ledger)..."
            className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-waypoint-primary"
          />
        </div>

        {/* Modal Actions */}
        <div className="flex items-center justify-end gap-3 pt-2 border-t border-slate-800">
          <Button type="button" variant="outline" size="sm" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button
            type="submit"
            variant="primary"
            size="sm"
            disabled={isSubmitting || isDemotingSelf || !reason.trim()}
          >
            {isSubmitting ? (
              <>
                <Loader2 className="w-4 h-4 mr-1.5 animate-spin" />
                Updating Role...
              </>
            ) : (
              'Confirm Role Change'
            )}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
