import React, { useState, useEffect, useCallback, useMemo } from 'react'
import { adminApi } from './adminApi'
import { useAuthStore } from '@/store/authStore'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import { DataTable } from '@/components/ui/DataTable'
import { ChangeRoleModal } from './components/ChangeRoleModal'
import { ProvisionUserModal } from './components/ProvisionUserModal'
import {
  Users, Shield, ShieldAlert, Bus, Scale, User, UserPlus,
  Unlock, Lock, CheckCircle2, AlertTriangle, RefreshCw, Search,
  PowerOff, Power, Filter
} from 'lucide-react'

export function AdminUsersPage() {
  const currentUser = useAuthStore((state) => state.user)

  const [users, setUsers] = useState([])
  const [isLoading, setIsLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState(null)
  const [successNotice, setSuccessNotice] = useState(null)

  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedRoleFilter, setSelectedRoleFilter] = useState('ALL')
  const [selectedStatusFilter, setSelectedStatusFilter] = useState('ALL')

  // Modals state
  const [roleModalUser, setRoleModalUser] = useState(null)
  const [isProvisionModalOpen, setIsProvisionModalOpen] = useState(false)

  const loadUsers = useCallback(async () => {
    if (currentUser && currentUser.role && currentUser.role !== 'Admin') {
      return
    }
    setIsLoading(true)
    setErrorMessage(null)
    try {
      const data = await adminApi.getUsers({
        pageSize: 100 // load up to 100 for client search and filtering in DataTable
      })
      const items = Array.isArray(data) ? data : (data?.items || [])
      setUsers(items)
    } catch (err) {
      setErrorMessage(err.response?.data?.detail || err.message || 'Failed to load user directory.')
    } finally {
      setIsLoading(false)
    }
  }, [currentUser])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  if (currentUser && currentUser.role && currentUser.role !== 'Admin') {
    return (
      <Card className="p-8 border-slate-200 bg-white text-center max-w-xl mx-auto mt-6 shadow-sm">
        <div className="w-14 h-14 rounded-2xl bg-amber-50 border border-amber-200 flex items-center justify-center mx-auto mb-4 text-amber-600 shadow-xs">
          <ShieldAlert className="w-7 h-7" />
        </div>
        <h2 className="text-xl font-bold text-slate-900 font-display">Administrator Access Required</h2>
        <p className="text-xs text-slate-600 mt-2.5 leading-relaxed">
          The User & Role Governance console is restricted to <strong>System Administrators</strong> (BR-ADMIN-001).
          You are currently signed in as <strong>{currentUser.fullName || currentUser.email}</strong> with role{' '}
          <span className="font-semibold text-slate-800">{currentUser.role}</span>.
        </p>
        <div className="mt-6 flex items-center justify-center gap-3">
          <Button variant="outline" size="sm" onClick={() => window.history.back()}>
            Go Back
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={() => {
              useAuthStore.getState().logout()
              window.location.href = '/login'
            }}
            className="font-bold shadow-sm"
          >
            Sign in as Administrator
          </Button>
        </div>
      </Card>
    )
  }

  // Telemetry Metrics
  const metrics = useMemo(() => {
    const total = users.length
    const admins = users.filter((u) => u.role === 'Admin' && u.isActive).length
    const operators = users.filter((u) => u.role === 'Operator' || u.role === 'TransportManager').length
    const lockedOrInactive = users.filter((u) => !u.isActive || u.isLocked).length

    return { total, admins, operators, lockedOrInactive }
  }, [users])

  // Filtered dataset
  const filteredUsers = useMemo(() => {
    return users.filter((u) => {
      const matchesSearch =
        !searchTerm ||
        u.fullName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.email?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.phoneNumber?.toLowerCase().includes(searchTerm.toLowerCase())

      const matchesRole =
        selectedRoleFilter === 'ALL' || u.role?.toLowerCase() === selectedRoleFilter.toLowerCase()

      const matchesStatus =
        selectedStatusFilter === 'ALL' ||
        (selectedStatusFilter === 'ACTIVE' && u.isActive && !u.isLocked) ||
        (selectedStatusFilter === 'LOCKED' && u.isLocked) ||
        (selectedStatusFilter === 'INACTIVE' && !u.isActive)

      return matchesSearch && matchesRole && matchesStatus
    })
  }, [users, searchTerm, selectedRoleFilter, selectedStatusFilter])

  // Quick Unlock Handler
  const handleQuickUnlock = async (user) => {
    try {
      const updated = await adminApi.updateUserStatus(user.id, {
        unlockAccount: true,
        reason: 'Administrative instant unlock from governance console'
      })
      setUsers((prev) => prev.map((u) => (u.id === user.id ? updated : u)))
      setSuccessNotice(`Account for ${user.fullName || user.email} successfully unlocked.`)
      setTimeout(() => setSuccessNotice(null), 4000)
    } catch (err) {
      setErrorMessage(err.response?.data?.detail || 'Failed to unlock account.')
    }
  }

  // Quick Toggle Status Handler
  const handleToggleStatus = async (user) => {
    const newStatus = !user.isActive
    const promptText = newStatus
      ? `Activate account for ${user.fullName}?`
      : `Deactivate account for ${user.fullName}? They will not be able to log in.`

    if (!window.confirm(promptText)) return

    try {
      const updated = await adminApi.updateUserStatus(user.id, {
        isActive: newStatus,
        reason: `Administrative status toggle to ${newStatus ? 'Active' : 'Inactive'}`
      })
      setUsers((prev) => prev.map((u) => (u.id === user.id ? updated : u)))
      setSuccessNotice(`Account status updated for ${user.fullName || user.email}.`)
      setTimeout(() => setSuccessNotice(null), 4000)
    } catch (err) {
      setErrorMessage(err.response?.data?.detail || 'Failed to update account status.')
    }
  }

  // Handle user updated from modal
  const handleUserRoleUpdated = (updatedUser) => {
    setUsers((prev) => prev.map((u) => (u.id === updatedUser.id ? updatedUser : u)))
    setSuccessNotice(`Role successfully changed to ${updatedUser.role} for ${updatedUser.fullName}.`)
    setTimeout(() => setSuccessNotice(null), 4000)
  }

  // Handle new user created
  const handleUserCreated = (newUser) => {
    setUsers((prev) => [newUser, ...prev])
    setSuccessNotice(`New user ${newUser.fullName} successfully provisioned with role ${newUser.role}.`)
    setTimeout(() => setSuccessNotice(null), 4000)
  }

  // Role Badge Helper
  const renderRoleBadge = (role) => {
    switch (role) {
      case 'Admin':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-purple-50 text-purple-700 border border-purple-200">
            <Shield className="w-3 h-3 text-purple-600" />
            Admin
          </span>
        )
      case 'TransportManager':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200">
            <Scale className="w-3 h-3 text-amber-600" />
            Transport Manager
          </span>
        )
      case 'Operator':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200">
            <Bus className="w-3 h-3 text-blue-600" />
            Operator
          </span>
        )
      default:
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
            <User className="w-3 h-3 text-emerald-600" />
            Passenger
          </span>
        )
    }
  }

  // Table Columns Definition
  const columns = [
    {
      header: 'User Identity',
      accessor: 'fullName',
      sortable: true,
      render: (row) => (
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-full bg-slate-100 border border-slate-200 flex items-center justify-center text-xs font-bold text-slate-700">
            {row.fullName ? row.fullName.slice(0, 2).toUpperCase() : 'US'}
          </div>
          <div>
            <div className="font-bold text-sm text-slate-900 flex items-center gap-1.5">
              {row.fullName}
              {currentUser?.email === row.email && (
                <span className="text-[10px] font-normal px-1.5 py-0.5 rounded bg-slate-100 text-slate-600 border border-slate-200">
                  You
                </span>
              )}
            </div>
            <div className="text-xs text-slate-500">{row.email}</div>
          </div>
        </div>
      )
    },
    {
      header: 'Assigned Role',
      accessor: 'role',
      sortable: true,
      render: (row) => renderRoleBadge(row.role)
    },
    {
      header: 'Account Status',
      accessor: 'isActive',
      sortable: true,
      render: (row) => {
        if (row.isLocked) {
          return (
            <div className="flex items-center gap-2">
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-red-50 text-red-700 border border-red-200">
                <Lock className="w-3 h-3" />
                Locked (BR-AUTH-002)
              </span>
              <Button
                variant="outline"
                size="sm"
                className="h-6 text-[10px] px-2 py-0 border-amber-300 text-amber-700 hover:bg-amber-50"
                onClick={() => handleQuickUnlock(row)}
              >
                <Unlock className="w-3 h-3 mr-1" />
                Unlock
              </Button>
            </div>
          )
        }
        if (!row.isActive) {
          return (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-slate-100 text-slate-600 border border-slate-200">
              <PowerOff className="w-3 h-3" />
              Deactivated
            </span>
          )
        }
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
            Active
          </span>
        )
      }
    },
    {
      header: 'Profile & Depot',
      accessor: 'profileMetadata',
      render: (row) => (
        <span className="text-xs text-slate-600 font-mono">
          {row.profileMetadata || '—'}
        </span>
      )
    },
    {
      header: 'Joined Date',
      accessor: 'createdAt',
      sortable: true,
      render: (row) => (
        <span className="text-xs text-slate-500">
          {row.createdAt ? new Date(row.createdAt).toLocaleDateString() : '—'}
        </span>
      )
    },
    {
      header: 'Administrative Actions',
      render: (row) => (
        <div className="flex items-center gap-1.5">
          <Button
            variant="outline"
            size="sm"
            className="text-xs px-2.5 py-1"
            onClick={() => setRoleModalUser(row)}
          >
            Edit Role
          </Button>
          <Button
            variant="outline"
            size="sm"
            className={`text-xs px-2 py-1 ${row.isActive ? 'hover:text-red-600' : 'hover:text-emerald-600'}`}
            title={row.isActive ? 'Deactivate Account' : 'Activate Account'}
            onClick={() => handleToggleStatus(row)}
          >
            {row.isActive ? <PowerOff className="w-3.5 h-3.5 text-slate-400 hover:text-red-600" /> : <Power className="w-3.5 h-3.5 text-emerald-600" />}
          </Button>
        </div>
      )
    }
  ]

  return (
    <div className="space-y-6">
      {/* Success Notice Banner */}
      {successNotice && (
        <div className="p-3.5 bg-emerald-50 border border-emerald-200 rounded-xl flex items-center gap-2.5 text-xs text-emerald-800 shadow-sm transition-all animate-fadeIn">
          <CheckCircle2 className="w-4 h-4 text-emerald-600 flex-shrink-0" />
          <span>{successNotice}</span>
        </div>
      )}

      {/* Error Alert */}
      {errorMessage && (
        <div className="p-3.5 bg-red-50 border border-red-200 rounded-xl flex items-center gap-2.5 text-xs text-red-800 shadow-sm transition-all animate-fadeIn">
          <AlertTriangle className="w-4 h-4 text-red-600 flex-shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Telemetry Metric Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm">
          <span className="text-xs font-medium text-slate-500">Total Registered Users</span>
          <div className="text-2xl font-bold font-display text-slate-900 mt-1">{metrics.total}</div>
          <span className="text-[10px] text-slate-500 mt-0.5 block">Across all 4 system roles</span>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-500">Active Administrators</span>
            {metrics.admins === 1 && (
              <span className="px-1.5 py-0.5 text-[9px] font-bold rounded bg-amber-50 text-amber-700 border border-amber-200">
                Sole Admin Risk
              </span>
            )}
          </div>
          <div className="text-2xl font-bold font-display text-slate-900 mt-1 flex items-center gap-2">
            <Shield className="w-5 h-5 text-purple-600" />
            {metrics.admins}
          </div>
          <span className="text-[10px] text-slate-500 mt-0.5 block">BR-ADMIN-001 protected</span>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm">
          <span className="text-xs font-medium text-slate-500">Operational Personnel</span>
          <div className="text-2xl font-bold font-display text-slate-900 mt-1 flex items-center gap-2">
            <Bus className="w-5 h-5 text-blue-600" />
            {metrics.operators}
          </div>
          <span className="text-[10px] text-slate-500 mt-0.5 block">Operators & Transport Managers</span>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm">
          <span className="text-xs font-medium text-slate-500">Restricted / Locked</span>
          <div className="text-2xl font-bold font-display text-slate-900 mt-1 flex items-center gap-2">
            <Lock className={`w-5 h-5 ${metrics.lockedOrInactive > 0 ? 'text-amber-500' : 'text-slate-400'}`} />
            {metrics.lockedOrInactive}
          </div>
          <span className="text-[10px] text-slate-500 mt-0.5 block">Inactive or 5 bad logins (BR-AUTH-002)</span>
        </div>
      </div>

      {/* Control Bar: Filters, Search & Provision CTA */}
      <div className="p-4 bg-white border border-slate-200 rounded-2xl flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3 shadow-sm">
        <div className="flex flex-1 flex-col sm:flex-row items-stretch sm:items-center gap-3">
          {/* Search Box */}
          <div className="relative flex-1">
            <Search className="w-4 h-4 absolute left-3 top-2.5 text-slate-400" />
            <input
              type="text"
              placeholder="Search by name, email, or phone..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full bg-slate-50 border border-slate-200 rounded-xl pl-9 pr-3 py-2 text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:border-waypoint-primary"
            />
          </div>

          {/* Role Filter */}
          <div className="flex items-center gap-1.5">
            <Filter className="w-3.5 h-3.5 text-slate-400 ml-1" />
            <select
              value={selectedRoleFilter}
              onChange={(e) => setSelectedRoleFilter(e.target.value)}
              className="bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-xs text-slate-900 focus:outline-none focus:border-waypoint-primary"
            >
              <option value="ALL">All Roles</option>
              <option value="Admin">Admin</option>
              <option value="TransportManager">Transport Manager</option>
              <option value="Operator">Operator</option>
              <option value="Passenger">Passenger</option>
            </select>
          </div>

          {/* Status Filter */}
          <select
            value={selectedStatusFilter}
            onChange={(e) => setSelectedStatusFilter(e.target.value)}
            className="bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-xs text-slate-900 focus:outline-none focus:border-waypoint-primary"
          >
            <option value="ALL">All Statuses</option>
            <option value="ACTIVE">Active Only</option>
            <option value="LOCKED">Locked Only</option>
            <option value="INACTIVE">Deactivated Only</option>
          </select>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-2 self-end md:self-auto">
          <Button variant="outline" size="sm" onClick={loadUsers} disabled={isLoading}>
            <RefreshCw className={`w-4 h-4 mr-1.5 ${isLoading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>

          <Button variant="primary" size="sm" onClick={() => setIsProvisionModalOpen(true)}>
            <UserPlus className="w-4 h-4 mr-1.5" />
            Provision Account
          </Button>
        </div>
      </div>

      {/* Directory Table */}
      <DataTable
        columns={columns}
        data={filteredUsers}
        isLoading={isLoading}
        pageSize={10}
        emptyTitle="No users matched your criteria"
        emptyDescription="Try clearing your search query or role/status filters."
        exportFilename="waypoint-user-directory.csv"
      />

      {/* Role Modification Modal */}
      <ChangeRoleModal
        isOpen={Boolean(roleModalUser)}
        onClose={() => setRoleModalUser(null)}
        targetUser={roleModalUser}
        currentUser={currentUser}
        onRoleUpdated={handleUserRoleUpdated}
      />

      {/* Direct User Provisioning Modal */}
      <ProvisionUserModal
        isOpen={isProvisionModalOpen}
        onClose={() => setIsProvisionModalOpen(false)}
        onUserCreated={handleUserCreated}
      />
    </div>
  )
}
export default AdminUsersPage
