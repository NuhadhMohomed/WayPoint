import React, { useState, useEffect, useCallback } from 'react'
import { fleetApi } from './fleetApi'
import { useFleetStore } from '@/store/fleetStore'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import {
  Bus, Plus, Wrench, Eye, Search,
  ChevronLeft, ChevronRight, X, CheckCircle2, AlertCircle, Loader2
} from 'lucide-react'

const BUS_CLASS_OPTIONS = [
  { value: 'Standard', label: 'Standard' },
  { value: 'SemiLuxury', label: 'Semi-Luxury' },
  { value: 'Luxury', label: 'Luxury' },
  { value: 'SuperLuxury', label: 'Super Luxury' },
]

const BUS_CLASS_COLORS = {
  Standard: 'bg-slate-500/20 text-slate-300 border-slate-500/30',
  SemiLuxury: 'bg-sky-500/20 text-sky-300 border-sky-500/30',
  Luxury: 'bg-amber-500/20 text-amber-300 border-amber-500/30',
  SuperLuxury: 'bg-purple-500/20 text-purple-300 border-purple-500/30',
}

export function FleetMatrixBuilderPage() {
  const { buses, busPagination, busesLoading, setBuses, setBusesLoading } = useFleetStore()
  const [searchTerm, setSearchTerm] = useState('')
  const [classFilter, setClassFilter] = useState('')
  const [maintenanceFilter, setMaintenanceFilter] = useState('')
  const [showAddModal, setShowAddModal] = useState(false)
  const [previewLayout, setPreviewLayout] = useState(null)
  const [previewLoading, setPreviewLoading] = useState(false)
  const [actionMessage, setActionMessage] = useState(null)
  const [seatLayouts, setSeatLayouts] = useState([])

  const fetchBuses = useCallback(async (pageNumber = 1) => {
    setBusesLoading(true)
    try {
      const params = { pageNumber, pageSize: 20 }
      if (searchTerm) params.searchTerm = searchTerm
      if (classFilter) params.busClass = classFilter
      if (maintenanceFilter !== '') params.isUnderMaintenance = maintenanceFilter === 'true'
      const data = await fleetApi.getBuses(params)
      setBuses(data)
    } catch (err) {
      setActionMessage({ type: 'error', text: err.response?.data?.detail || 'Failed to load buses' })
    } finally {
      setBusesLoading(false)
    }
  }, [searchTerm, classFilter, maintenanceFilter, setBuses, setBusesLoading])

  useEffect(() => {
    fetchBuses()
    fleetApi.getSeatLayouts().then(setSeatLayouts).catch(() => {})
  }, [fetchBuses])

  const handleToggleMaintenance = async (bus) => {
    try {
      await fleetApi.updateBusMaintenance(bus.id, {
        isUnderMaintenance: !bus.isUnderMaintenance,
        description: bus.isUnderMaintenance ? 'Returned to service' : 'Entered maintenance',
      })
      setActionMessage({ type: 'success', text: `${bus.registrationNumber} ${bus.isUnderMaintenance ? 'returned to service' : 'placed under maintenance'}` })
      fetchBuses(busPagination.pageNumber)
    } catch (err) {
      setActionMessage({ type: 'error', text: err.response?.data?.detail || 'Failed to toggle maintenance' })
    }
    setTimeout(() => setActionMessage(null), 4000)
  }

  const handleViewLayout = async (bus) => {
    if (!bus.seatLayoutId) return
    setPreviewLoading(true)
    try {
      const layout = await fleetApi.getSeatLayoutById(bus.seatLayoutId)
      setPreviewLayout(layout)
    } catch {
      setActionMessage({ type: 'error', text: 'Failed to load seat layout' })
    } finally {
      setPreviewLoading(false)
    }
  }

  return (
    <div className="space-y-5">
      {/* Action Message Toast */}
      {actionMessage && (
        <div className={`flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm border animate-in fade-in slide-in-from-top-2 ${
          actionMessage.type === 'success'
            ? 'bg-emerald-950/60 border-emerald-800 text-emerald-300'
            : 'bg-rose-950/60 border-rose-800 text-rose-300'
        }`}>
          {actionMessage.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
          {actionMessage.text}
        </div>
      )}

      {/* Stats Row */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Total Fleet', value: busPagination.totalCount, color: 'text-waypoint-blue' },
          { label: 'Active', value: buses.filter(b => !b.isUnderMaintenance).length, color: 'text-emerald-400' },
          { label: 'Maintenance', value: buses.filter(b => b.isUnderMaintenance).length, color: 'text-amber-400' },
          { label: 'Layouts', value: seatLayouts.length, color: 'text-indigo-400' },
        ].map((stat) => (
          <div key={stat.label} className="p-4 rounded-xl bg-slate-950 border border-slate-800">
            <div className={`text-2xl font-bold font-display ${stat.color}`}>{stat.value}</div>
            <div className="text-xs text-slate-400 mt-1">{stat.label}</div>
          </div>
        ))}
      </div>

      {/* Fleet Table Card */}
      <Card>
        <CardHeader
          title="Bus Fleet Inventory"
          subtitle="Manage fleet vehicles, classes, and maintenance status"
          action={
            <Button size="sm" onClick={() => setShowAddModal(true)}>
              <Plus className="w-4 h-4 mr-1.5" /> Add Bus
            </Button>
          }
        />

        {/* Filters */}
        <div className="flex items-center gap-3 mb-5">
          <div className="relative flex-1 max-w-xs">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
            <input
              type="text"
              placeholder="Search by registration..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-9 pr-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue focus:border-waypoint-blue"
            />
          </div>
          <select
            value={classFilter}
            onChange={(e) => setClassFilter(e.target.value)}
            className="px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-slate-200 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
          >
            <option value="">All Classes</option>
            {BUS_CLASS_OPTIONS.map(c => (
              <option key={c.value} value={c.value}>{c.label}</option>
            ))}
          </select>
          <select
            value={maintenanceFilter}
            onChange={(e) => setMaintenanceFilter(e.target.value)}
            className="px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-slate-200 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
          >
            <option value="">All Status</option>
            <option value="false">Active</option>
            <option value="true">Under Maintenance</option>
          </select>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-xs text-slate-400 uppercase tracking-wider border-b border-slate-800">
                <th className="text-left py-3 px-3 font-medium">Registration</th>
                <th className="text-left py-3 px-3 font-medium">Class</th>
                <th className="text-center py-3 px-3 font-medium">Capacity</th>
                <th className="text-left py-3 px-3 font-medium">Seat Layout</th>
                <th className="text-center py-3 px-3 font-medium">Status</th>
                <th className="text-right py-3 px-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {busesLoading ? (
                <tr>
                  <td colSpan={6} className="text-center py-16">
                    <Loader2 className="w-6 h-6 animate-spin text-waypoint-blue mx-auto" />
                    <p className="text-xs text-slate-500 mt-2">Loading fleet...</p>
                  </td>
                </tr>
              ) : buses.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-16">
                    <Bus className="w-8 h-8 text-slate-700 mx-auto mb-2" />
                    <p className="text-sm text-slate-500">No buses found</p>
                    <p className="text-xs text-slate-600 mt-1">Add a bus to get started</p>
                  </td>
                </tr>
              ) : (
                buses.map((bus) => (
                  <tr key={bus.id} className="border-b border-slate-800/50 hover:bg-slate-950/50 transition-colors">
                    <td className="py-3 px-3">
                      <span className="font-mono font-semibold text-white">{bus.registrationNumber}</span>
                    </td>
                    <td className="py-3 px-3">
                      <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded border ${BUS_CLASS_COLORS[bus.busClass] || BUS_CLASS_COLORS.Standard}`}>
                        {bus.busClass}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-center text-slate-300">{bus.totalSeatCapacity}</td>
                    <td className="py-3 px-3 text-slate-400 text-xs">
                      {bus.seatLayoutName || <span className="text-slate-600 italic">No layout</span>}
                    </td>
                    <td className="py-3 px-3 text-center">
                      {bus.isUnderMaintenance ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
                          <Wrench className="w-3 h-3" /> Maintenance
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                          <CheckCircle2 className="w-3 h-3" /> Active
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-3 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        {bus.seatLayoutId && (
                          <button
                            onClick={() => handleViewLayout(bus)}
                            className="p-1.5 rounded-md text-slate-400 hover:text-indigo-300 hover:bg-indigo-500/10 transition-colors"
                            title="View seat layout"
                          >
                            <Eye className="w-4 h-4" />
                          </button>
                        )}
                        <button
                          onClick={() => handleToggleMaintenance(bus)}
                          className={`p-1.5 rounded-md transition-colors ${
                            bus.isUnderMaintenance
                              ? 'text-emerald-400 hover:bg-emerald-500/10'
                              : 'text-amber-400 hover:bg-amber-500/10'
                          }`}
                          title={bus.isUnderMaintenance ? 'Return to service' : 'Place under maintenance'}
                        >
                          <Wrench className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {busPagination.totalPages > 1 && (
          <div className="flex items-center justify-between mt-5 pt-4 border-t border-slate-800">
            <span className="text-xs text-slate-500">
              Page {busPagination.pageNumber} of {busPagination.totalPages} ({busPagination.totalCount} buses)
            </span>
            <div className="flex items-center gap-2">
              <Button
                variant="outline" size="sm"
                disabled={busPagination.pageNumber <= 1}
                onClick={() => fetchBuses(busPagination.pageNumber - 1)}
              >
                <ChevronLeft className="w-4 h-4" />
              </Button>
              <Button
                variant="outline" size="sm"
                disabled={busPagination.pageNumber >= busPagination.totalPages}
                onClick={() => fetchBuses(busPagination.pageNumber + 1)}
              >
                <ChevronRight className="w-4 h-4" />
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Seat Layout Preview Modal */}
      {previewLayout && (
        <SeatLayoutPreviewModal layout={previewLayout} onClose={() => setPreviewLayout(null)} />
      )}

      {/* Add Bus Modal */}
      {showAddModal && (
        <AddBusModal
          seatLayouts={seatLayouts}
          onClose={() => setShowAddModal(false)}
          onSuccess={() => {
            setShowAddModal(false)
            fetchBuses()
            setActionMessage({ type: 'success', text: 'Bus added to fleet!' })
            setTimeout(() => setActionMessage(null), 4000)
          }}
          onError={(msg) => {
            setActionMessage({ type: 'error', text: msg })
            setTimeout(() => setActionMessage(null), 4000)
          }}
        />
      )}
    </div>
  )
}

// ─── Add Bus Modal ───

function AddBusModal({ seatLayouts, onClose, onSuccess, onError }) {
  const [form, setForm] = useState({
    registrationNumber: '',
    busClass: 'Standard',
    totalSeatCapacity: 40,
    seatLayoutId: '',
  })
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setSubmitting(true)
    try {
      await fleetApi.createBus({
        ...form,
        seatLayoutId: form.seatLayoutId || null,
      })
      onSuccess()
    } catch (err) {
      onError(err.response?.data?.detail || 'Failed to create bus')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-5">
          <h3 className="text-lg font-semibold font-display text-white">Register New Bus</h3>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-white">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Registration Number</label>
            <input
              type="text"
              required
              placeholder="e.g. WP-KA-1234"
              value={form.registrationNumber}
              onChange={(e) => setForm({ ...form, registrationNumber: e.target.value })}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1.5">Bus Class</label>
              <select
                value={form.busClass}
                onChange={(e) => setForm({ ...form, busClass: e.target.value })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
              >
                {BUS_CLASS_OPTIONS.map(c => (
                  <option key={c.value} value={c.value}>{c.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1.5">Seat Capacity</label>
              <input
                type="number"
                min={1}
                required
                value={form.totalSeatCapacity}
                onChange={(e) => setForm({ ...form, totalSeatCapacity: parseInt(e.target.value) || 1 })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Seat Layout Template</label>
            <select
              value={form.seatLayoutId}
              onChange={(e) => setForm({ ...form, seatLayoutId: e.target.value })}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            >
              <option value="">— No layout —</option>
              {seatLayouts.map(l => (
                <option key={l.id} value={l.id}>{l.name} ({l.totalRows}×{l.totalColumns}, {l.totalSeats} seats)</option>
              ))}
            </select>
          </div>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button variant="outline" size="sm" type="button" onClick={onClose}>Cancel</Button>
            <Button size="sm" type="submit" disabled={submitting}>
              {submitting ? <Loader2 className="w-4 h-4 animate-spin mr-1.5" /> : <Plus className="w-4 h-4 mr-1.5" />}
              {submitting ? 'Registering...' : 'Register Bus'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── Seat Layout Preview Modal ───

function SeatLayoutPreviewModal({ layout, onClose }) {
  const seatColorMap = {
    Standard: 'bg-waypoint-blue/80 border-waypoint-blue',
    Window: 'bg-sky-600/80 border-sky-500',
    Aisle: 'bg-slate-600/80 border-slate-500',
    FrontRow: 'bg-amber-600/80 border-amber-500',
    VIP: 'bg-purple-600/80 border-purple-500',
  }

  // Build 2D grid
  const grid = Array.from({ length: layout.totalRows }, () =>
    Array.from({ length: layout.totalColumns }, () => null)
  )

  layout.seats.forEach((seat) => {
    if (seat.rowIndex < layout.totalRows && seat.columnIndex < layout.totalColumns) {
      grid[seat.rowIndex][seat.columnIndex] = seat
    }
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-lg p-6">
        <div className="flex items-center justify-between mb-5">
          <div>
            <h3 className="text-lg font-semibold font-display text-white">{layout.name}</h3>
            <p className="text-xs text-slate-400">{layout.totalRows} rows × {layout.totalColumns} columns • {layout.seats.length} seats</p>
          </div>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-white">
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Bus Front Indicator */}
        <div className="text-center mb-3">
          <div className="inline-block px-4 py-1 rounded-t-xl bg-slate-800 text-[10px] text-slate-400 uppercase tracking-wider font-medium">
            🚌 Front
          </div>
        </div>

        {/* Seat Grid */}
        <div className="flex flex-col items-center gap-1.5 p-4 bg-slate-950 rounded-xl border border-slate-800">
          {grid.map((row, rowIdx) => (
            <div key={rowIdx} className="flex items-center gap-1.5">
              <span className="text-[10px] text-slate-600 w-5 text-right font-mono">{rowIdx + 1}</span>
              {row.map((seat, colIdx) => {
                // Add aisle gap in the middle for 4-column layouts
                const showAisle = layout.totalColumns === 4 && colIdx === 2

                return (
                  <React.Fragment key={colIdx}>
                    {showAisle && <div className="w-4" />}
                    {seat ? (
                      <div
                        className={`w-10 h-10 rounded-lg border flex items-center justify-center text-[10px] font-bold text-white cursor-default ${
                          seatColorMap[seat.seatClass] || seatColorMap.Standard
                        }`}
                        title={`${seat.seatNumber} (${seat.seatClass})`}
                      >
                        {seat.seatNumber}
                      </div>
                    ) : (
                      <div className="w-10 h-10 rounded-lg border border-dashed border-slate-800" />
                    )}
                  </React.Fragment>
                )
              })}
            </div>
          ))}
        </div>

        {/* Legend */}
        <div className="flex items-center justify-center gap-4 mt-4">
          {Object.entries(seatColorMap).map(([cls, color]) => (
            <div key={cls} className="flex items-center gap-1.5">
              <div className={`w-3 h-3 rounded-sm border ${color}`} />
              <span className="text-[10px] text-slate-400">{cls}</span>
            </div>
          ))}
        </div>

        <div className="flex justify-end mt-5">
          <Button variant="outline" size="sm" onClick={onClose}>Close</Button>
        </div>
      </div>
    </div>
  )
}
