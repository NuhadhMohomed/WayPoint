import React, { useState } from 'react'
import { Modal } from '@/components/ui/Modal'
import { Button } from '@/components/ui/Button'
import { adminApi } from '../adminApi'
import { UserPlus, Loader2 } from 'lucide-react'

export function ProvisionUserModal({ isOpen, onClose, onUserCreated }) {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [role, setRole] = useState('Operator')
  const [operatorCode, setOperatorCode] = useState('')
  const [companyName, setCompanyName] = useState('')
  const [assignedRegion, setAssignedRegion] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState(null)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setIsSubmitting(true)
    setErrorMessage(null)

    try {
      const payload = {
        fullName: fullName.trim(),
        email: email.trim().toLowerCase(),
        password,
        phoneNumber: phoneNumber.trim() || undefined,
        role,
        operatorCode: role === 'Operator' ? operatorCode.trim() : undefined,
        companyName: role === 'Operator' ? companyName.trim() : undefined,
        assignedRegion: role === 'Operator' ? assignedRegion.trim() : undefined
      }

      const created = await adminApi.createUser(payload)
      onUserCreated(created)
      onClose()
      // Reset form
      setFullName('')
      setEmail('')
      setPassword('')
      setPhoneNumber('')
      setRole('Operator')
      setOperatorCode('')
      setCompanyName('')
      setAssignedRegion('')
    } catch (err) {
      setErrorMessage(err.response?.data?.detail || err.response?.data?.title || 'Failed to provision user.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Provision System Account"
      subtitle="Directly provision enterprise operators, managers, or administrative personnel"
      maxWidth="max-w-lg"
    >
      <form onSubmit={handleSubmit} className="p-6 space-y-4">
        {errorMessage && (
          <div className="p-3 bg-red-50 border border-red-200 rounded-xl text-xs text-red-700">
            {errorMessage}
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Full Name *</label>
            <input
              type="text"
              required
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="e.g. Kasun Perera"
              className="w-full bg-white border border-slate-200 rounded-xl px-3 py-2 text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-primary shadow-2xs"
            />
          </div>
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Email Address *</label>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="e.g. kasun@transit.lk"
              className="w-full bg-white border border-slate-200 rounded-xl px-3 py-2 text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-primary shadow-2xs"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Initial Password *</label>
            <input
              type="password"
              required
              minLength={6}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Secure password (e.g. Pass123!)"
              className="w-full bg-white border border-slate-200 rounded-xl px-3 py-2 text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-primary shadow-2xs"
            />
          </div>
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Phone Number</label>
            <input
              type="tel"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              placeholder="+94771234567"
              className="w-full bg-white border border-slate-200 rounded-xl px-3 py-2 text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-primary shadow-2xs"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">Initial System Role *</label>
          <select
            value={role}
            onChange={(e) => setRole(e.target.value)}
            className="w-full bg-white border border-slate-200 rounded-xl px-3 py-2 text-xs text-slate-900 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-primary shadow-2xs"
          >
            <option value="Operator">Transit Operator (Fleet & Depot Dispatch)</option>
            <option value="TransportManager">Transport Manager (Authority & Approvals)</option>
            <option value="Admin">System Administrator</option>
          </select>
        </div>

        {/* Dynamic Operator Profile */}
        {role === 'Operator' && (
          <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-3">
            <span className="text-xs font-bold text-slate-800 block">Operator Depot Assignment</span>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="block text-[11px] font-medium text-slate-600 mb-1">Operator Code</label>
                <input
                  type="text"
                  placeholder="e.g. OP-KANDY-02"
                  value={operatorCode}
                  onChange={(e) => setOperatorCode(e.target.value)}
                  className="w-full bg-white border border-slate-200 rounded-lg px-2.5 py-1.5 text-xs text-slate-900 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-primary shadow-2xs"
                />
              </div>
              <div>
                <label className="block text-[11px] font-medium text-slate-600 mb-1">Company / Entity</label>
                <input
                  type="text"
                  placeholder="e.g. Central Province Transit"
                  value={companyName}
                  onChange={(e) => setCompanyName(e.target.value)}
                  className="w-full bg-white border border-slate-200 rounded-lg px-2.5 py-1.5 text-xs text-slate-900 focus:outline-none focus:ring-2 focus:ring-waypoint-blue/20 focus:border-waypoint-primary shadow-2xs"
                />
              </div>
            </div>
          </div>
        )}

        <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-200">
          <Button type="button" variant="outline" size="sm" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" size="sm" disabled={isSubmitting}>
            {isSubmitting ? (
              <>
                <Loader2 className="w-4 h-4 mr-1.5 animate-spin" />
                Provisioning...
              </>
            ) : (
              <>
                <UserPlus className="w-4 h-4 mr-1.5" />
                Create Account
              </>
            )}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
