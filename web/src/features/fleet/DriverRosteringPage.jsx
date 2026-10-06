import React, { useState, useEffect, useCallback } from 'react'
import { fleetApi } from './fleetApi'
import { useFleetStore } from '../../store/fleetStore'
import { Card, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { downloadCsv } from '../../lib/csvExport'
import {
  Users, Plus, Search, ChevronLeft, ChevronRight,
  X, CheckCircle2, AlertCircle, AlertTriangle,
  Loader2, Calendar, Clock, Download, ShieldCheck, ShieldAlert
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

  const handleExportCsv = () => {
    const exportData = drivers.map((d) => ({
      FullName: d.fullName,
      LicenseNumber: d.licenseNumber,
      ContactNumber: d.contactNumber,
      Status: d.status,
      AssignmentsCount: d.assignmentCount,
    }))
    downloadCsv(exportData, 'waypoint_driver_roster.csv')
  }

  return (
    <div className="space-y-5">
      {/* Toast Notification */}
      {message && (
        <div
          role="status"
          className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold border ${
            message.type === 'success'
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-red-50 border-red-200 text-red-800'
          }`}
        >
          {message.type === 'success' ? <CheckCircle2 className="w-4 h-4 text-emerald-600" /> : <AlertCircle className="w-4 h-4 text-red-600" />}
          <span>{message.text}</span>
        </div>
      )}

      {/* Stats Row */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {[
          { label: 'Total Licensed Drivers', value: driverPagination.totalCount, color: 'text-waypoint-primary' },
          { label: 'Active on Shifts', value: drivers.filter(d => d.status === 'Active').length, color: 'text-emerald-600' },
          { label: 'Total Scheduled Runs', value: drivers.reduce((sum, d) => sum + d.assignmentCount, 0), color: 'text-sky-600' },
        ].map((stat) => (
          <div key={stat.label} className="p-4 rounded-2xl bg-white border border-slate-200 shadow-sm">
            <div className={`text-2xl font-black font-mono ${stat.color}`}>{stat.value}</div>
            <div className="text-xs text-slate-500 mt-1 font-medium">{stat.label}</div>
          </div>
        ))}
      </div>

      {/* Driver Table Card */}
      <Card className="p-5 border-slate-200 bg-white shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-5 border-b border-slate-100 pb-4">
          <div>
            <h3 className="text-lg font-bold font-display text-slate-900">Driver Roster & Shift Assignments</h3>
            <p className="text-xs text-slate-500 mt-0.5">
              Enforce mandatory 8-hour rest periods (<strong>BR-RESOURCE-002</strong>) and track active dispatch hours.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={handleExportCsv}
              disabled={drivers.length === 0}
              className="gap-1.5 text-xs"
            >
              <Download className="w-3.5 h-3.5" />
              Export Roster
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowAssignModal(true)}
              className="gap-1.5 text-xs"
            >
              <Calendar className="w-3.5 h-3.5" /> Assign Shift
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={() => setShowAddModal(true)}
              className="gap-1.5 font-bold shadow-md shadow-waypoint-primary/10"
            >
              <Plus className="w-4 h-4" /> Add Driver
            </Button>
          </div>
        </div>

        {/* Search */}
        <div className="flex items-center gap-3 mb-5">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              placeholder="Search driver by name or license..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-9 pr-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            />
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-xs text-left">
            <thead>
              <tr className="text-[11px] text-slate-500 uppercase tracking-wider border-b border-slate-200 bg-slate-50/50 font-semibold">
                <th className="py-3 px-3">Driver Name</th>
                <th className="py-3 px-3">License Number</th>
                <th className="py-3 px-3">Phone</th>
                <th className="text-center py-3 px-3">Status</th>
                <th className="text-center py-3 px-3">HOS Shifts</th>
                <th className="text-right py-3 px-3">Timeline Gantt</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 font-mono">
              {driversLoading ? (
                <tr>
                  <td colSpan={6} className="text-center py-16">
                    <Loader2 className="w-6 h-6 animate-spin text-waypoint-primary mx-auto" />
                    <p className="text-xs text-slate-500 mt-2 font-sans">Loading driver telemetry...</p>
                  </td>
                </tr>
              ) : drivers.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-16">
                    <Users className="w-8 h-8 text-slate-400 mx-auto mb-2" />
                    <p className="text-sm font-semibold text-slate-700 font-sans">No drivers found</p>
                    <p className="text-xs text-slate-500 mt-1 font-sans">Add a licensed driver to get started</p>
                  </td>
                </tr>
              ) : (
                drivers.map((driver) => (
                  <tr key={driver.id} className="hover:bg-slate-50/80 transition-colors">
                    <td className="py-3 px-3">
                      <span className="font-bold text-slate-900 font-sans text-xs">{driver.fullName}</span>
                    </td>
                    <td className="py-3 px-3 text-slate-700 font-mono">{driver.licenseNumber}</td>
                    <td className="py-3 px-3 text-slate-500">{driver.contactNumber || '—'}</td>
                    <td className="py-3 px-3 text-center font-sans">
                      <span className={`inline-flex px-2 py-0.5 text-[11px] font-bold rounded-full border ${
                        driver.status === 'Active'
                          ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                          : 'bg-slate-100 text-slate-600 border-slate-200'
                      }`}>
                        {driver.status}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-center">
                      <span className="inline-flex items-center gap-1 px-2.5 py-0.5 text-xs font-bold rounded-lg bg-slate-100 text-slate-800 border border-slate-200">
                        {driver.assignmentCount} Runs
                      </span>
                    </td>
                    <td className="py-3 px-3 text-right">
                      <button
                        onClick={() => handleViewGantt(driver)}
                        className="p-1.5 rounded-lg text-slate-400 hover:text-waypoint-primary hover:bg-slate-100 transition-colors"
                        title="View HOS Rest Compliance Timeline"
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
          <div className="flex items-center justify-between mt-5 pt-4 border-t border-slate-100">
            <span className="text-xs text-slate-500 font-mono">
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
            showToast('success', 'Driver registered successfully!')
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
            showToast('success', 'Driver assigned to scheduled corridor service!')
          }}
          onError={(msg) => showToast('error', msg)}
        />
      )}
    </div>
  )
}

function DriverGanttChart({ assignments, loading, onClose }) {
  if (loading) {
    return (
      <Card className="p-8 text-center border-slate-200 bg-white">
        <Loader2 className="w-6 h-6 animate-spin text-waypoint-primary mx-auto" />
        <p className="text-xs text-slate-500 mt-2">Computing HOS rest telemetry...</p>
      </Card>
    )
  }

  const driverName = assignments.length > 0 ? assignments[0].driverName : 'Driver'

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

  if (sortedAssignments.length === 0) {
    return (
      <Card className="p-6 border-slate-200 bg-white">
        <div className="flex items-center justify-between border-b border-slate-100 pb-3 mb-4">
          <h3 className="text-sm font-bold text-slate-900 font-display">Schedule Timeline: {driverName}</h3>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-slate-600"><X className="w-4 h-4" /></button>
        </div>
        <div className="text-center py-8">
          <Calendar className="w-8 h-8 text-slate-400 mx-auto mb-2" />
          <p className="text-xs text-slate-500">No active corridor assignments mapped to this driver yet</p>
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
    <Card className="p-6 border-slate-200 bg-white shadow-sm">
      <div className="flex items-center justify-between border-b border-slate-100 pb-3 mb-4">
        <div>
          <h3 className="text-base font-bold text-slate-900 font-display flex items-center gap-2">
            <Clock className="w-4 h-4 text-waypoint-primary" />
            Hours-of-Service (HOS) Rest Compliance: {driverName}
          </h3>
          <p className="text-xs text-slate-500 mt-0.5">
            Colored bars indicate driving shifts. Red alerts highlight violations of the mandatory 8-hour rest rule (<strong>BR-RESOURCE-002</strong>).
          </p>
        </div>
        <button onClick={onClose} className="p-1 text-slate-400 hover:text-slate-600"><X className="w-4 h-4" /></button>
      </div>

      <div className="space-y-3">
        {sortedAssignments.map((assignment, idx) => {
          const depTime = new Date(assignment.departureTime).getTime()
          const arrTime = new Date(assignment.arrivalTime).getTime()
          const left = ((depTime - minTime) / totalRange) * 100
          const width = Math.max(((arrTime - depTime) / totalRange) * 100, 3)
          const restGap = restGaps.find(g => g.afterIndex === idx)

          return (
            <div key={assignment.id} className="space-y-1.5">
              <div className="relative h-12 bg-slate-50 rounded-xl border border-slate-200 overflow-hidden shadow-inner">
                <div
                  className="absolute top-1 bottom-1 rounded-lg bg-waypoint-primary text-waypoint-onPrimary flex items-center justify-between text-xs font-mono font-bold px-3 shadow-sm"
                  style={{ left: `${left}%`, width: `${width}%`, minWidth: '80px' }}
                >
                  <span className="truncate">{assignment.serviceCode}</span>
                  <span className="text-[10px] opacity-80">{formatTime(assignment.departureTime)}</span>
                </div>
              </div>

              {restGap && (
                <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-mono">
                  {restGap.compliant ? (
                    <div className="flex items-center gap-1.5 text-emerald-700">
                      <ShieldCheck className="w-3.5 h-3.5" />
                      <span>{restGap.hours.toFixed(1)}h Rest Gap (Compliant with BR-RESOURCE-002 $\ge$ 8h)</span>
                    </div>
                  ) : (
                    <div className="flex items-center gap-1.5 text-red-700 bg-red-50 border border-red-200 px-2 py-0.5 rounded">
                      <ShieldAlert className="w-3.5 h-3.5" />
                      <span className="font-bold">VIOLATION: Only {restGap.hours.toFixed(1)}h rest gap before next departure! (&lt; 8h mandatory)</span>
                    </div>
                  )}
                </div>
              )}
            </div>
          )
        })}
      </div>
    </Card>
  )
}

function AddDriverModal({ onClose, onSuccess, onError }) {
  const [form, setForm] = useState({
    fullName: '',
    licenseNumber: '',
    contactNumber: '',
  })
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div className="bg-white border border-slate-200 rounded-2xl shadow-2xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-4 border-b border-slate-100 pb-3">
          <h3 className="text-base font-bold font-display text-slate-900">Register Licensed Driver</h3>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">Full Legal Name</label>
            <input
              type="text"
              required
              placeholder="e.g. Sunil Gunawardena"
              value={form.fullName}
              onChange={(e) => setForm({ ...form, fullName: e.target.value })}
              className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">NTC Heavy Vehicle License</label>
            <input
              type="text"
              required
              placeholder="e.g. B1234567"
              value={form.licenseNumber}
              onChange={(e) => setForm({ ...form, licenseNumber: e.target.value })}
              className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">Contact Phone Number</label>
            <input
              type="tel"
              placeholder="+94 77 987 6543"
              value={form.contactNumber}
              onChange={(e) => setForm({ ...form, contactNumber: e.target.value })}
              className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            />
          </div>

          <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
            <Button variant="outline" size="sm" type="button" onClick={onClose}>Cancel</Button>
            <Button variant="primary" size="sm" type="submit" isLoading={submitting} className="font-bold">
              Register Driver
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

function AssignDriverModal({ drivers, onClose, onSuccess, onError }) {
  const [form, setForm] = useState({
    driverId: '',
    serviceCode: '',
    departureTime: '',
    arrivalTime: '',
  })
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div className="bg-white border border-slate-200 rounded-2xl shadow-2xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-4 border-b border-slate-100 pb-3">
          <h3 className="text-base font-bold font-display text-slate-900">Assign Driver to Service</h3>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">Select Driver</label>
            <select
              required
              value={form.driverId}
              onChange={(e) => setForm({ ...form, driverId: e.target.value })}
              className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            >
              <option value="">— Select licensed driver —</option>
              {drivers.map(d => (
                <option key={d.id} value={d.id}>{d.fullName} ({d.licenseNumber})</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">Service Code</label>
            <input
              type="text"
              required
              placeholder="e.g. SRV-COL-ELLA-0630"
              value={form.serviceCode}
              onChange={(e) => setForm({ ...form, serviceCode: e.target.value })}
              className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1.5">Departure Time</label>
              <input
                type="datetime-local"
                required
                value={form.departureTime}
                onChange={(e) => setForm({ ...form, departureTime: e.target.value })}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1.5">Arrival Time</label>
              <input
                type="datetime-local"
                required
                value={form.arrivalTime}
                onChange={(e) => setForm({ ...form, arrivalTime: e.target.value })}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
              />
            </div>
          </div>

          <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
            <Button variant="outline" size="sm" type="button" onClick={onClose}>Cancel</Button>
            <Button variant="primary" size="sm" type="submit" isLoading={submitting} className="font-bold">
              Confirm Assignment
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
