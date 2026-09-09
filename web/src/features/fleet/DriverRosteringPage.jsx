import React, { useState, useEffect, useCallback } from 'react'
import { fleetApi } from './fleetApi'
import { useFleetStore } from '@/store/fleetStore'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import {
  Users, Plus, Search, ChevronLeft, ChevronRight,
  X, CheckCircle2, AlertCircle, AlertTriangle,
  Loader2, Calendar, Clock
} from 'lucide-react'

export function DriverRosteringPage() {
  const { drivers, driverPagination, driversLoading, setDrivers, setDriversLoading } = useFleetStore()
  const [searchTerm, setSearchTerm] = useState('')
  const [showAddModal, setShowAddModal] = useState(false)
  const [showAssignModal, setShowAssignModal] = useState(false)
  const [ganttDriverId, setGanttDriverId] = useState(null)
  const [ganttData, setGanttData] = useState([])
  const [ganttLoading, setGanttLoading] = useState(false)
  const [message, setMessage] = useState(null)

  const fetchDrivers = useCallback(async (pageNumber = 1) => {
    setDriversLoading(true)
    try {
      const params = { pageNumber, pageSize: 20 }
      if (searchTerm) params.searchTerm = searchTerm
      const data = await fleetApi.getDrivers(params)
      setDrivers(data)
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.detail || 'Failed to load drivers' })
    } finally {
      setDriversLoading(false)
    }
  }, [searchTerm, setDrivers, setDriversLoading])

  useEffect(() => { fetchDrivers() }, [fetchDrivers])

  const showToast = (type, text) => {
    setMessage({ type, text })
    setTimeout(() => setMessage(null), 4000)
  }

  const handleViewGantt = async (driver) => {
    setGanttDriverId(driver.id)
    setGanttLoading(true)
    try {
      const assignments = await fleetApi.getDriverAssignments(driver.id)
      setGanttData(assignments)
    } catch {
      showToast('error', 'Failed to load driver assignments')
    } finally {
      setGanttLoading(false)
    }
  }

  return (
    <div className="space-y-5">
      {/* Toast */}
      {message && (
        <div className={`flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm border ${
          message.type === 'success'
            ? 'bg-emerald-950/60 border-emerald-800 text-emerald-300'
            : 'bg-rose-950/60 border-rose-800 text-rose-300'
        }`}>
          {message.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
          {message.text}
        </div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Total Drivers', value: driverPagination.totalCount, color: 'text-waypoint-blue' },
          { label: 'Active', value: drivers.filter(d => d.status === 'Active').length, color: 'text-emerald-400' },
          { label: 'Total Assignments', value: drivers.reduce((sum, d) => sum + d.assignmentCount, 0), color: 'text-indigo-400' },
        ].map((stat) => (
          <div key={stat.label} className="p-4 rounded-xl bg-slate-950 border border-slate-800">
            <div className={`text-2xl font-bold font-display ${stat.color}`}>{stat.value}</div>
            <div className="text-xs text-slate-400 mt-1">{stat.label}</div>
          </div>
        ))}
      </div>

      {/* Driver Table */}
      <Card>
        <CardHeader
          title="Driver Directory"
          subtitle="Manage drivers, view assignments, and check rest compliance"
          action={
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowAssignModal(true)}>
                <Calendar className="w-4 h-4 mr-1.5" /> Assign Driver
              </Button>
              <Button size="sm" onClick={() => setShowAddModal(true)}>
                <Plus className="w-4 h-4 mr-1.5" /> Add Driver
              </Button>
            </div>
          }
        />

        {/* Search */}
        <div className="mb-5">
          <div className="relative max-w-xs">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
            <input
              type="text"
              placeholder="Search by name, license..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-9 pr-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            />
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-xs text-slate-400 uppercase tracking-wider border-b border-slate-800">
                <th className="text-left py-3 px-3 font-medium">Name</th>
                <th className="text-left py-3 px-3 font-medium">License #</th>
                <th className="text-left py-3 px-3 font-medium">Phone</th>
                <th className="text-center py-3 px-3 font-medium">Status</th>
                <th className="text-center py-3 px-3 font-medium">Assignments</th>
                <th className="text-right py-3 px-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {driversLoading ? (
                <tr>
                  <td colSpan={6} className="text-center py-16">
                    <Loader2 className="w-6 h-6 animate-spin text-waypoint-blue mx-auto" />
                    <p className="text-xs text-slate-500 mt-2">Loading drivers...</p>
                  </td>
                </tr>
              ) : drivers.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-16">
                    <Users className="w-8 h-8 text-slate-700 mx-auto mb-2" />
                    <p className="text-sm text-slate-500">No drivers found</p>
                  </td>
                </tr>
              ) : (
                drivers.map((driver) => (
                  <tr key={driver.id} className="border-b border-slate-800/50 hover:bg-slate-950/50 transition-colors">
                    <td className="py-3 px-3 font-medium text-white">{driver.fullName}</td>
                    <td className="py-3 px-3 font-mono text-xs text-slate-400">{driver.licenseNumber}</td>
                    <td className="py-3 px-3 text-slate-400 text-xs">{driver.phoneNumber || '—'}</td>
                    <td className="py-3 px-3 text-center">
                      <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded border ${
                        driver.status === 'Active'
                          ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30'
                          : 'bg-slate-500/20 text-slate-300 border-slate-500/30'
                      }`}>
                        {driver.status}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-center">
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded bg-indigo-500/15 text-indigo-300 border border-indigo-500/30">
                        {driver.assignmentCount}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-right">
                      <button
                        onClick={() => handleViewGantt(driver)}
                        className="p-1.5 rounded-md text-slate-400 hover:text-indigo-300 hover:bg-indigo-500/10 transition-colors"
                        title="View schedule timeline"
                      >
                        <Clock className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {driverPagination.totalPages > 1 && (
          <div className="flex items-center justify-between mt-5 pt-4 border-t border-slate-800">
            <span className="text-xs text-slate-500">
              Page {driverPagination.pageNumber} of {driverPagination.totalPages}
            </span>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" disabled={driverPagination.pageNumber <= 1} onClick={() => fetchDrivers(driverPagination.pageNumber - 1)}>
                <ChevronLeft className="w-4 h-4" />
              </Button>
              <Button variant="outline" size="sm" disabled={driverPagination.pageNumber >= driverPagination.totalPages} onClick={() => fetchDrivers(driverPagination.pageNumber + 1)}>
                <ChevronRight className="w-4 h-4" />
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Gantt Chart Card */}
      {ganttDriverId && (
        <DriverGanttChart
          assignments={ganttData}
          loading={ganttLoading}
          onClose={() => { setGanttDriverId(null); setGanttData([]) }}
        />
      )}

      {/* Add Driver Modal */}
      {showAddModal && (
        <AddDriverModal
          onClose={() => setShowAddModal(false)}
          onSuccess={() => {
            setShowAddModal(false)
            fetchDrivers()
            showToast('success', 'Driver added successfully!')
          }}
          onError={(msg) => showToast('error', msg)}
        />
      )}

      {/* Assign Driver Modal */}
      {showAssignModal && (
        <AssignDriverModal
          drivers={drivers}
          onClose={() => setShowAssignModal(false)}
          onSuccess={() => {
            setShowAssignModal(false)
            fetchDrivers()
            showToast('success', 'Driver assigned to service!')
          }}
          onError={(msg) => showToast('error', msg)}
        />
      )}
    </div>
  )
}

// ─── Driver Gantt Chart ───

function DriverGanttChart({ assignments, loading, onClose }) {
  if (loading) {
    return (
      <Card>
        <div className="text-center py-8">
          <Loader2 className="w-6 h-6 animate-spin text-waypoint-blue mx-auto" />
          <p className="text-xs text-slate-500 mt-2">Loading schedule...</p>
        </div>
      </Card>
    )
  }

  const driverName = assignments.length > 0 ? assignments[0].driverName : 'Driver'

  // Calculate rest gaps between consecutive assignments
  const sortedAssignments = [...assignments].sort(
    (a, b) => new Date(a.departureTime) - new Date(b.departureTime)
  )

  const restGaps = []
  for (let i = 0; i < sortedAssignments.length - 1; i++) {
    const arrival = new Date(sortedAssignments[i].arrivalTime)
    const nextDeparture = new Date(sortedAssignments[i + 1].departureTime)
    const hours = (nextDeparture - arrival) / (1000 * 60 * 60)
    restGaps.push({ afterIndex: i, hours, compliant: hours >= 8 })
  }

  // Find time range for the Gantt
  if (sortedAssignments.length === 0) {
    return (
      <Card>
        <CardHeader title={`Schedule: ${driverName}`} action={
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-white"><X className="w-5 h-5" /></button>
        } />
        <div className="text-center py-8">
          <Calendar className="w-8 h-8 text-slate-700 mx-auto mb-2" />
          <p className="text-sm text-slate-500">No assignments yet</p>
        </div>
      </Card>
    )
  }

  const minTime = new Date(sortedAssignments[0].departureTime).getTime()
  const maxTime = new Date(sortedAssignments[sortedAssignments.length - 1].arrivalTime).getTime()
  const totalRange = Math.max(maxTime - minTime, 1)

  const formatTime = (dateStr) => {
    const d = new Date(dateStr)
    return d.toLocaleString('en-LK', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
  }

  return (
    <Card>
      <CardHeader
        title={`Rest Compliance Gantt: ${driverName}`}
        subtitle="Colored bars show assigned shifts. Red warnings indicate < 8h rest gaps (BR-RESOURCE-002)."
        action={
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-white"><X className="w-5 h-5" /></button>
        }
      />

      {/* Timeline */}
      <div className="space-y-3">
        {sortedAssignments.map((assignment, idx) => {
          const depTime = new Date(assignment.departureTime).getTime()
          const arrTime = new Date(assignment.arrivalTime).getTime()
          const left = ((depTime - minTime) / totalRange) * 100
          const width = Math.max(((arrTime - depTime) / totalRange) * 100, 3)
          const restGap = restGaps.find(g => g.afterIndex === idx)

          return (
            <div key={assignment.id}>
              {/* Assignment bar */}
              <div className="relative h-12 bg-slate-950 rounded-lg border border-slate-800 overflow-hidden">
                <div
                  className="absolute top-1 bottom-1 rounded-md bg-waypoint-blue/80 border border-waypoint-blue flex items-center justify-center text-[10px] text-white font-medium px-2 overflow-hidden whitespace-nowrap"
                  style={{ left: `${left}%`, width: `${width}%`, minWidth: '60px' }}
                  title={`${assignment.serviceCode}: ${formatTime(assignment.departureTime)} → ${formatTime(assignment.arrivalTime)}`}
                >
                  {assignment.serviceCode}
                </div>
              </div>

              {/* Time labels */}
              <div className="flex items-center justify-between mt-1 px-1">
                <span className="text-[10px] text-slate-500">{formatTime(assignment.departureTime)}</span>
                <span className="text-[10px] text-slate-500">{formatTime(assignment.arrivalTime)}</span>
              </div>

              {/* Rest gap indicator */}
              {restGap && (
                <div className={`flex items-center gap-1.5 mt-2 mb-1 px-2 py-1.5 rounded text-xs border ${
                  restGap.compliant
                    ? 'bg-emerald-950/40 border-emerald-800/50 text-emerald-400'
                    : 'bg-rose-950/40 border-rose-800/50 text-rose-400'
                }`}>
                  {restGap.compliant
                    ? <CheckCircle2 className="w-3.5 h-3.5" />
                    : <AlertTriangle className="w-3.5 h-3.5" />
                  }
                  <span className="font-medium">{restGap.hours.toFixed(1)}h rest</span>
                  <span className="text-slate-500">
                    {restGap.compliant ? '— compliant' : '— VIOLATION (min 8h required)'}
                  </span>
                </div>
              )}
            </div>
          )
        })}
      </div>

      {/* Summary */}
      <div className="flex items-center gap-4 mt-5 pt-4 border-t border-slate-800">
        <div className="flex items-center gap-1.5 text-xs text-slate-400">
          <div className="w-3 h-3 rounded-sm bg-waypoint-blue/80 border border-waypoint-blue" />
          Active Shift
        </div>
        <div className="flex items-center gap-1.5 text-xs text-emerald-400">
          <CheckCircle2 className="w-3 h-3" /> ≥ 8h Rest (Compliant)
        </div>
        <div className="flex items-center gap-1.5 text-xs text-rose-400">
          <AlertTriangle className="w-3 h-3" /> &lt; 8h Rest (Violation)
        </div>
      </div>
    </Card>
  )
}

// ─── Add Driver Modal ───

function AddDriverModal({ onClose, onSuccess, onError }) {
  const [form, setForm] = useState({ fullName: '', licenseNumber: '', phoneNumber: '' })
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setSubmitting(true)
    try {
      await fleetApi.createDriver(form)
      onSuccess()
    } catch (err) {
      onError(err.response?.data?.detail || 'Failed to create driver')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-5">
          <h3 className="text-lg font-semibold font-display text-white">Add New Driver</h3>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-white"><X className="w-5 h-5" /></button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Full Name</label>
            <input
              type="text" required placeholder="e.g. Kamal Perera"
              value={form.fullName}
              onChange={(e) => setForm({ ...form, fullName: e.target.value })}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">License Number</label>
            <input
              type="text" required placeholder="e.g. DL-2024-A1234"
              value={form.licenseNumber}
              onChange={(e) => setForm({ ...form, licenseNumber: e.target.value })}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Phone Number</label>
            <input
              type="text" placeholder="e.g. +94 77 123 4567"
              value={form.phoneNumber}
              onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button variant="outline" size="sm" type="button" onClick={onClose}>Cancel</Button>
            <Button size="sm" type="submit" disabled={submitting}>
              {submitting ? <Loader2 className="w-4 h-4 animate-spin mr-1.5" /> : <Plus className="w-4 h-4 mr-1.5" />}
              {submitting ? 'Adding...' : 'Add Driver'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── Assign Driver Modal ───

function AssignDriverModal({ drivers, onClose, onSuccess, onError }) {
  const [form, setForm] = useState({ driverId: '', serviceId: '' })
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!form.driverId || !form.serviceId) {
      onError('Please select both a driver and a service')
      return
    }
    setSubmitting(true)
    try {
      await fleetApi.assignDriver(form)
      onSuccess()
    } catch (err) {
      onError(err.response?.data?.detail || 'Failed to assign driver')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-5">
          <h3 className="text-lg font-semibold font-display text-white">Assign Driver to Service</h3>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-white"><X className="w-5 h-5" /></button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Select Driver</label>
            <select
              value={form.driverId}
              onChange={(e) => setForm({ ...form, driverId: e.target.value })}
              required
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            >
              <option value="">— Select a driver —</option>
              {drivers.filter(d => d.status === 'Active').map(d => (
                <option key={d.id} value={d.id}>{d.fullName} ({d.licenseNumber})</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Service ID</label>
            <input
              type="text" required placeholder="Paste Service UUID from Sethum's routes"
              value={form.serviceId}
              onChange={(e) => setForm({ ...form, serviceId: e.target.value })}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            />
            <p className="text-[10px] text-slate-600 mt-1">
              Overlap & 8h rest checks are enforced server-side (BR-TIME-001 / BR-RESOURCE-002)
            </p>
          </div>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button variant="outline" size="sm" type="button" onClick={onClose}>Cancel</Button>
            <Button size="sm" type="submit" disabled={submitting}>
              {submitting ? <Loader2 className="w-4 h-4 animate-spin mr-1.5" /> : <Calendar className="w-4 h-4 mr-1.5" />}
              {submitting ? 'Assigning...' : 'Assign Driver'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
