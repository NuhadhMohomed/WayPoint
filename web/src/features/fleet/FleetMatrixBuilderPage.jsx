import React, { useState, useEffect, useCallback } from 'react'
import { fleetApi } from './fleetApi'
import { useFleetStore } from '../../store/fleetStore'
import { Card, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { downloadCsv } from '../../lib/csvExport'
import {
  Bus, Plus, Wrench, Eye, Search,
  ChevronLeft, ChevronRight, X, CheckCircle2, AlertCircle, Loader2,
  Download, Wifi, Wind, Zap, Navigation
} from 'lucide-react'

const BUS_CLASS_OPTIONS = [
  { value: 'Standard', label: 'Standard' },
  { value: 'SemiLuxury', label: 'Semi-Luxury' },
  { value: 'Luxury', label: 'Luxury' },
  { value: 'SuperLuxury', label: 'Super Luxury' },
]

const BUS_CLASS_COLORS = {
  Standard: 'bg-slate-800 text-slate-300 border-slate-700',
  SemiLuxury: 'bg-sky-500/15 text-sky-300 border-sky-500/30',
  Luxury: 'bg-amber-500/15 text-amber-300 border-amber-500/30',
  SuperLuxury: 'bg-purple-500/15 text-purple-300 border-purple-500/30',
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

  const handleExportCsv = () => {
    const exportData = buses.map((b) => ({
      Registration: b.registrationNumber,
      Class: b.busClass,
      SeatCapacity: b.totalSeatCapacity,
      Layout: b.seatLayoutName || 'Unassigned',
      Status: b.isUnderMaintenance ? 'Maintenance' : 'Active',
    }))
    downloadCsv(exportData, 'waypoint_bus_fleet.csv')
  }

  return (
    <div className="space-y-5">
      {/* Action Message Toast */}
      {actionMessage && (
        <div
          role="status"
          className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold border animate-in fade-in slide-in-from-top-2 ${
            actionMessage.type === 'success'
              ? 'bg-emerald-950/70 border-emerald-800 text-emerald-200'
              : 'bg-red-950/70 border-red-800 text-red-200'
          }`}
        >
          {actionMessage.type === 'success' ? <CheckCircle2 className="w-4 h-4 text-emerald-400" /> : <AlertCircle className="w-4 h-4 text-red-400" />}
          <span>{actionMessage.text}</span>
        </div>
      )}

      {/* Stats Row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {[
          { label: 'Total Fleet', value: busPagination.totalCount, color: 'text-waypoint-primary' },
          { label: 'Active in Service', value: buses.filter(b => !b.isUnderMaintenance).length, color: 'text-emerald-400' },
          { label: 'Depot Maintenance', value: buses.filter(b => b.isUnderMaintenance).length, color: 'text-amber-400' },
          { label: 'Layout Blueprints', value: seatLayouts.length, color: 'text-sky-400' },
        ].map((stat) => (
          <div key={stat.label} className="p-4 rounded-2xl bg-slate-950 border border-slate-800 shadow-inner">
            <div className={`text-2xl font-black font-mono ${stat.color}`}>{stat.value}</div>
            <div className="text-xs text-slate-400 mt-1 font-medium">{stat.label}</div>
          </div>
        ))}
      </div>

      {/* Fleet Table Card */}
      <Card className="p-5 border-slate-800 bg-slate-900/90 shadow-xl">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-5 border-b border-slate-800 pb-4">
          <div>
            <h3 className="text-lg font-bold font-display text-white">Bus Fleet Inventory</h3>
            <p className="text-xs text-slate-400 mt-0.5">Manage fleet vehicles, luxury classes, and maintenance schedules</p>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={handleExportCsv}
              disabled={buses.length === 0}
              className="gap-1.5 text-xs"
            >
              <Download className="w-3.5 h-3.5" />
              Export CSV
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={() => setShowAddModal(true)}
              className="gap-1.5 font-bold shadow-lg shadow-waypoint-primary/20"
            >
              <Plus className="w-4 h-4" /> Add Bus
            </Button>
          </div>
        </div>

        {/* Filters */}
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 mb-5">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
            <input
              type="text"
              placeholder="Search by registration (e.g. WP-KA)..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-9 pr-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            />
          </div>
          <select
            value={classFilter}
            onChange={(e) => setClassFilter(e.target.value)}
            className="px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-slate-200 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
          >
            <option value="">All Classes</option>
            {BUS_CLASS_OPTIONS.map(c => (
              <option key={c.value} value={c.value}>{c.label}</option>
            ))}
          </select>
          <select
            value={maintenanceFilter}
            onChange={(e) => setMaintenanceFilter(e.target.value)}
            className="px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-slate-200 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
          >
            <option value="">All Status</option>
            <option value="false">Active Only</option>
            <option value="true">Under Maintenance</option>
          </select>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-xs text-left">
            <thead>
              <tr className="text-[11px] text-slate-400 uppercase tracking-wider border-b border-slate-800 bg-slate-950/60 font-semibold">
                <th className="py-3 px-3">Registration</th>
                <th className="py-3 px-3">Class</th>
                <th className="text-center py-3 px-3">Capacity</th>
                <th className="py-3 px-3">Seat Layout Blueprint</th>
                <th className="text-center py-3 px-3">Status</th>
                <th className="text-right py-3 px-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60 font-mono">
              {busesLoading ? (
                <tr>
                  <td colSpan={6} className="text-center py-16">
                    <Loader2 className="w-6 h-6 animate-spin text-waypoint-primary mx-auto" />
                    <p className="text-xs text-slate-400 mt-2 font-sans">Loading fleet telemetry...</p>
                  </td>
                </tr>
              ) : buses.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-16">
                    <Bus className="w-8 h-8 text-slate-700 mx-auto mb-2" />
                    <p className="text-sm font-semibold text-slate-300 font-sans">No buses registered in directory</p>
                    <p className="text-xs text-slate-500 mt-1 font-sans">Add a bus or adjust filter criteria</p>
                  </td>
                </tr>
              ) : (
                buses.map((bus) => (
                  <tr key={bus.id} className="hover:bg-slate-950/60 transition-colors">
                    <td className="py-3 px-3">
                      <span className="font-bold text-white tracking-wide">{bus.registrationNumber}</span>
                    </td>
                    <td className="py-3 px-3">
                      <span className={`inline-flex px-2 py-0.5 text-[11px] font-bold rounded-md border ${BUS_CLASS_COLORS[bus.busClass] || BUS_CLASS_COLORS.Standard}`}>
                        {bus.busClass}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-center text-slate-300 font-bold">{bus.totalSeatCapacity}</td>
                    <td className="py-3 px-3 text-slate-400 font-sans">
                      {bus.seatLayoutName ? (
                        <span className="text-slate-200">{bus.seatLayoutName}</span>
                      ) : (
                        <span className="text-slate-600 italic">No layout mapped</span>
                      )}
                    </td>
                    <td className="py-3 px-3 text-center font-sans">
                      {bus.isUnderMaintenance ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 text-[11px] font-bold rounded-full bg-amber-500/20 text-amber-300 border border-amber-500/30">
                          <Wrench className="w-3 h-3" /> Maintenance
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 text-[11px] font-bold rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                          <CheckCircle2 className="w-3 h-3" /> In Service
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-3 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        {bus.seatLayoutId && (
                          <button
                            onClick={() => handleViewLayout(bus)}
                            className="p-1.5 rounded-lg text-slate-400 hover:text-waypoint-primary hover:bg-slate-800 transition-colors"
                            title="View seat layout blueprint"
                          >
                            <Eye className="w-4 h-4" />
                          </button>
                        )}
                        <button
                          onClick={() => handleToggleMaintenance(bus)}
                          className={`p-1.5 rounded-lg transition-colors ${
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
            <span className="text-xs text-slate-400 font-mono">
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
            setActionMessage({ type: 'success', text: 'Bus registered and added to fleet!' })
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
      <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-5 border-b border-slate-800 pb-3">
          <h3 className="text-base font-bold font-display text-white">Register New Bus</h3>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-white">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">Registration Number</label>
            <input
              type="text"
              required
              placeholder="e.g. WP-KA-1234"
              value={form.registrationNumber}
              onChange={(e) => setForm({ ...form, registrationNumber: e.target.value })}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs font-mono text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">Bus Class</label>
              <select
                value={form.busClass}
                onChange={(e) => setForm({ ...form, busClass: e.target.value })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-white focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
              >
                {BUS_CLASS_OPTIONS.map(c => (
                  <option key={c.value} value={c.value}>{c.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">Seat Capacity</label>
              <input
                type="number"
                min={1}
                required
                value={form.totalSeatCapacity}
                onChange={(e) => setForm({ ...form, totalSeatCapacity: parseInt(e.target.value) || 1 })}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs font-mono text-white focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">Seat Layout Template</label>
            <select
              value={form.seatLayoutId}
              onChange={(e) => setForm({ ...form, seatLayoutId: e.target.value })}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-white focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
            >
              <option value="">— No layout mapped —</option>
              {seatLayouts.map(l => (
                <option key={l.id} value={l.id}>{l.name} ({l.totalRows}×{l.totalColumns}, {l.totalSeats} seats)</option>
              ))}
            </select>
          </div>

          <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-800">
            <Button variant="outline" size="sm" type="button" onClick={onClose}>Cancel</Button>
            <Button variant="primary" size="sm" type="submit" isLoading={submitting} className="font-bold">
              Commit Bus to Fleet
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

function SeatLayoutPreviewModal({ layout, onClose }) {
  const seatColorMap = {
    Standard: 'bg-slate-800 border-slate-700 text-slate-200',
    Window: 'bg-sky-600/80 border-sky-400 text-white',
    Aisle: 'bg-slate-700/80 border-slate-600 text-slate-300',
    FrontRow: 'bg-amber-600/80 border-amber-400 text-white',
    VIP: 'bg-waypoint-amber border-amber-400 text-slate-950 font-bold',
  }

  const grid = Array.from({ length: layout.totalRows }, () =>
    Array.from({ length: layout.totalColumns }, () => null)
  )

  layout.seats.forEach((seat) => {
    if (seat.rowIndex < layout.totalRows && seat.columnIndex < layout.totalColumns) {
      grid[seat.rowIndex][seat.columnIndex] = seat
    }
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4" onClick={onClose}>
      <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-lg p-6" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between mb-4 border-b border-slate-800 pb-3">
          <div>
            <h3 className="text-base font-bold font-display text-white">{layout.name}</h3>
            <p className="text-xs text-slate-400 font-mono">{layout.totalRows} rows × {layout.totalColumns} cols • {layout.seats.length} total seats</p>
          </div>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-white">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="text-center mb-3">
          <div className="inline-block px-3 py-1 rounded-full bg-slate-800 text-[10px] text-slate-300 uppercase tracking-wider font-semibold font-mono">
            🚍 Front Driver Cabin
          </div>
        </div>

        <div className="flex flex-col items-center gap-1.5 p-4 bg-slate-950 rounded-2xl border border-slate-800 max-h-96 overflow-y-auto shadow-inner">
          {grid.map((row, rowIdx) => (
            <div key={rowIdx} className="flex items-center gap-1.5">
              <span className="text-[10px] text-slate-600 w-5 text-right font-mono font-bold">{rowIdx + 1}</span>
              {row.map((seat, colIdx) => {
                const showAisle = layout.totalColumns === 4 && colIdx === 2
                return (
                  <React.Fragment key={colIdx}>
                    {showAisle && <div className="w-4" />}
                    {seat ? (
                      <div
                        className={`w-9 h-9 rounded-lg border flex items-center justify-center text-[10px] font-bold cursor-default ${
                          seatColorMap[seat.seatClass] || seatColorMap.Standard
                        }`}
                        title={`${seat.seatNumber} (${seat.seatClass})`}
                      >
                        {seat.seatNumber}
                      </div>
                    ) : (
                      <div className="w-9 h-9 rounded-lg border border-dashed border-slate-800/80" />
                    )}
                  </React.Fragment>
                )
              })}
            </div>
          ))}
        </div>

        <div className="flex justify-end mt-4 pt-3 border-t border-slate-800">
          <Button variant="outline" size="sm" onClick={onClose}>Close</Button>
        </div>
      </div>
    </div>
  )
}
