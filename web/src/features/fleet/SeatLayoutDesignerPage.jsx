import React, { useState, useEffect } from 'react'
import { fleetApi } from './fleetApi'
import { Card, CardHeader } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import {
  LayoutGrid, Plus, X, Save, Trash2,
  CheckCircle2, AlertCircle, Loader2, RotateCcw, Eye,
  Compass, Sparkles, Filter
} from 'lucide-react'

const SEAT_TYPES = [
  { key: 'empty', label: 'Aisle / Empty', color: 'border-dashed border-slate-300 bg-slate-50 text-transparent' },
  { key: 'Standard', label: 'Standard', color: 'bg-slate-100 border-slate-300 text-slate-800' },
  { key: 'Window', label: 'Window Seat', color: 'bg-sky-50 border-sky-300 text-sky-800' },
  { key: 'VIP', label: 'VIP / Luxury', color: 'bg-amber-100 border-amber-300 text-amber-900 font-bold' },
]

function generateSeatNumber(rowIdx, colIdx) {
  const letters = 'ABCDEFGHIJ'
  return `${rowIdx + 1}${letters[colIdx] || colIdx}`
}

export function SeatLayoutDesignerPage() {
  const [layouts, setLayouts] = useState([])
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState(null)

  // Designer state
  const [showDesigner, setShowDesigner] = useState(false)
  const [layoutName, setLayoutName] = useState('')
  const [rows, setRows] = useState(10)
  const [cols, setCols] = useState(4)
  const [grid, setGrid] = useState([])
  const [saving, setSaving] = useState(false)
  const [highlightFilter, setHighlightFilter] = useState('ALL')

  // Preview state
  const [previewLayout, setPreviewLayout] = useState(null)
  const [blueprintLoading, setBlueprintLoading] = useState(false)

  const fetchLayouts = async () => {
    setLoading(true)
    try {
      const data = await fleetApi.getSeatLayouts()
      setLayouts(Array.isArray(data) ? data : (data?.items || []))
    } catch {
      setMessage({ type: 'error', text: 'Failed to load seat layouts' })
    } finally {
      setLoading(false)
    }
  }

  const handleViewBlueprint = async (layout) => {
    if (layout.seats && layout.seats.length > 0) {
      setPreviewLayout(layout)
      return
    }
    setBlueprintLoading(true)
    try {
      const fullLayout = await fleetApi.getSeatLayoutById(layout.id)
      setPreviewLayout(fullLayout || layout)
    } catch {
      setMessage({ type: 'error', text: 'Failed to load blueprint details' })
    } finally {
      setBlueprintLoading(false)
    }
  }

  useEffect(() => { fetchLayouts() }, [])

  // Initialize grid when rows/cols change
  const initializeGrid = () => {
    const newGrid = Array.from({ length: rows }, (_, r) =>
      Array.from({ length: cols }, (_, c) => ({
        type: 'Standard',
        seatNumber: generateSeatNumber(r, c),
      }))
    )
    setGrid(newGrid)
  }

  const handleStartDesigner = () => {
    setShowDesigner(true)
    setLayoutName('')
    setRows(10)
    setCols(4)
    initializeGrid()
  }

  useEffect(() => {
    if (showDesigner) initializeGrid()
  }, [rows, cols, showDesigner])

  const toggleCell = (rowIdx, colIdx) => {
    setGrid(prev => {
      const newGrid = prev.map(row => [...row])
      const cell = newGrid[rowIdx][colIdx]
      const typeOrder = ['Standard', 'Window', 'VIP', 'empty']
      const currentIdx = typeOrder.indexOf(cell.type)
      const nextType = typeOrder[(currentIdx + 1) % typeOrder.length]

      newGrid[rowIdx][colIdx] = {
        type: nextType,
        seatNumber: nextType === 'empty' ? '' : generateSeatNumber(rowIdx, colIdx),
      }
      return newGrid
    })
  }

  const totalSeats = grid.flat().filter(c => c.type !== 'empty').length

  const handleSave = async () => {
    if (!layoutName.trim()) {
      setMessage({ type: 'error', text: 'Layout name is required' })
      return
    }

    if (totalSeats === 0) {
      setMessage({ type: 'error', text: 'Layout must have at least one seat' })
      return
    }

    setSaving(true)
    try {
      const seats = []
      grid.forEach((row, r) => {
        row.forEach((cell, c) => {
          if (cell.type !== 'empty') {
            seats.push({
              seatNumber: cell.seatNumber,
              rowIndex: r,
              columnIndex: c,
              seatClass: cell.type,
            })
          }
        })
      })

      await fleetApi.createSeatLayout({
        name: layoutName.trim(),
        totalRows: rows,
        totalColumns: cols,
        seats,
      })

      setMessage({ type: 'success', text: `Layout "${layoutName}" created with ${seats.length} seats!` })
      setShowDesigner(false)
      fetchLayouts()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.detail || 'Failed to save layout' })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Toast Notification */}
      {message && (
        <div
          role="status"
          className={`flex items-center gap-2 px-4 py-3 rounded-xl text-xs font-semibold border ${
            message.type === 'success'
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-red-50 border-red-200 text-red-800'
          }`}
        >
          {message.type === 'success' ? <CheckCircle2 className="w-4 h-4 text-emerald-600" /> : <AlertCircle className="w-4 h-4 text-red-600" />}
          <span>{message.text}</span>
        </div>
      )}

      {/* Main Designer Screen */}
      {showDesigner ? (
        <Card className="p-6 border-slate-200 bg-white shadow-sm">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-100 pb-4 mb-6">
            <div>
              <h2 className="text-xl font-bold text-slate-900 tracking-tight font-display">
                Visual Seat Layout Designer
              </h2>
              <p className="text-xs text-slate-500 mt-1">
                Configure bus dimensions and click seats to cycle types (Standard → Window → VIP → Aisle).
              </p>
            </div>

            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowDesigner(false)}
              >
                Cancel
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={handleSave}
                isLoading={saving}
                className="gap-1.5 font-bold"
              >
                <Save className="w-4 h-4" />
                Save Layout
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
            {/* Left Controls Column */}
            <div className="space-y-5 lg:col-span-1">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                  Layout Template Name
                </label>
                <input
                  type="text"
                  placeholder="e.g. Standard 2×2 (40 seats)"
                  value={layoutName}
                  onChange={(e) => setLayoutName(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Rows
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="15"
                    value={rows}
                    onChange={(e) => setRows(Math.max(1, parseInt(e.target.value) || 1))}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary text-center"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                    Columns
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="6"
                    value={cols}
                    onChange={(e) => setCols(Math.max(1, parseInt(e.target.value) || 1))}
                    className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs font-mono text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary text-center"
                  />
                </div>
              </div>

              {/* Total Seats Counter */}
              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 shadow-sm">
                <span className="text-xs text-slate-500 font-medium">Total Seats</span>
                <div className="text-3xl font-black font-mono text-waypoint-primary mt-1">
                  {totalSeats}
                </div>
                <div className="text-[11px] text-slate-500 mt-1">
                  Active Passenger Capacity
                </div>
              </div>

              {/* Highlight Filters */}
              <div>
                <span className="block text-[11px] font-bold uppercase tracking-wider text-slate-500 mb-2">
                  Highlight Filter:
                </span>
                <div className="flex flex-wrap gap-1.5">
                  {['ALL', 'Window', 'VIP', 'Standard'].map((h) => (
                    <button
                      key={h}
                      type="button"
                      onClick={() => setHighlightFilter(h)}
                      className={`px-2.5 py-1 rounded-lg text-xs font-semibold transition-all ${
                        highlightFilter === h
                          ? 'bg-waypoint-primary text-waypoint-onPrimary'
                          : 'bg-slate-100 text-slate-600 border border-slate-200 hover:text-slate-900'
                      }`}
                    >
                      {h}
                    </button>
                  ))}
                </div>
              </div>

              {/* Seat Legend */}
              <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-200 space-y-2">
                <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500 block mb-1">
                  Seat Tier Legend:
                </span>
                {SEAT_TYPES.map((type) => (
                  <div key={type.key} className="flex items-center gap-2 text-xs">
                    <span className={`w-4 h-4 rounded border text-center text-[10px] flex items-center justify-center ${type.color}`}>
                      {type.key === 'empty' ? '' : '•'}
                    </span>
                    <span className="text-slate-700">{type.label}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Right Bus Grid Canvas */}
            <div className="lg:col-span-3 flex flex-col items-center justify-center p-6 rounded-2xl bg-slate-50 border border-slate-200 shadow-inner min-h-[420px]">
              {/* Bus Front Windshield / Driver Indicator */}
              <div className="w-full max-w-sm mb-4 pb-3 border-b-2 border-dashed border-slate-300 flex items-center justify-between text-xs text-slate-500 font-mono">
                <span className="flex items-center gap-1.5 font-bold text-slate-700">
                  <Compass className="w-4 h-4 text-waypoint-primary" /> FRONT / DRIVER CABIN
                </span>
                <span>ENTRY DOOR ➔</span>
              </div>

              <div
                className="grid gap-2 p-4 bg-white rounded-2xl border border-slate-200 shadow-sm overflow-x-auto max-w-full"
                style={{
                  gridTemplateColumns: `repeat(${cols}, minmax(44px, 54px))`,
                }}
              >
                {grid.map((row, r) =>
                  row.map((cell, c) => {
                    const isHighlighted = highlightFilter === 'ALL' || cell.type === highlightFilter
                    const typeConfig = SEAT_TYPES.find(t => t.key === cell.type) || SEAT_TYPES[0]

                    return (
                      <button
                        key={`${r}-${c}`}
                        type="button"
                        onClick={() => toggleCell(r, c)}
                        title={`Row ${r + 1}, Col ${c + 1} — ${cell.type} (click to change)`}
                        className={`h-11 w-11 sm:h-12 sm:w-12 rounded-xl text-xs font-mono font-bold flex items-center justify-center transition-all cursor-pointer border ${typeConfig.color} ${
                          isHighlighted ? 'opacity-100 ring-2 ring-waypoint-primary/40' : 'opacity-40'
                        } hover:scale-105 active:scale-95`}
                      >
                        {cell.seatNumber}
                      </button>
                    )
                  })
                )}
              </div>

              <div className="w-full max-w-sm mt-4 pt-3 border-t-2 border-dashed border-slate-300 text-center text-xs text-slate-500 font-mono">
                REAR ENGINE / BACK ROW
              </div>
            </div>
          </div>
        </Card>
      ) : (
        /* Templates List */
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-slate-900 tracking-tight font-display">
                Seat Layout Templates
              </h2>
              <p className="text-xs text-slate-500 mt-1">
                Pre-configured seating matrices mapped to luxury, semi-luxury, and highway express buses.
              </p>
            </div>

            <Button
              variant="primary"
              size="sm"
              onClick={handleStartDesigner}
              className="gap-1.5 font-bold shadow-md shadow-waypoint-primary/10"
            >
              <Plus className="w-4 h-4" />
              New Layout
            </Button>
          </div>

          {loading ? (
            <div className="flex items-center justify-center p-12 text-slate-500 gap-2">
              <Loader2 className="w-5 h-5 animate-spin text-waypoint-primary" />
              <span>Loading seat layouts...</span>
            </div>
          ) : layouts.length === 0 ? (
            <Card className="p-8 text-center text-slate-500 border border-slate-200 bg-white rounded-2xl">
              <LayoutGrid className="w-8 h-8 text-slate-400 mx-auto mb-3" />
              <p className="text-sm font-semibold text-slate-700">No seat layouts defined yet</p>
              <p className="text-xs text-slate-500 mt-1">Click "New Layout" above to design your first bus seating configuration.</p>
            </Card>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {layouts.map((layout) => (
                <Card
                  key={layout.id}
                  className="p-5 flex flex-col justify-between hover:border-slate-300 transition-all shadow-sm group bg-white"
                >
                  <div>
                    <div className="flex items-start justify-between gap-3 mb-2">
                      <span className="px-2 py-0.5 rounded-md bg-waypoint-primary/10 text-waypoint-primary border border-waypoint-primary/30 font-mono font-bold text-xs">
                        {layout.totalRows} × {layout.totalColumns} Grid
                      </span>
                      <span className="text-xs font-mono font-bold text-slate-700">
                        {layout.totalSeats ?? layout.seats?.length ?? 0} Seats
                      </span>
                    </div>

                    <h3 className="text-base font-bold text-slate-900 mb-1 group-hover:text-waypoint-primary transition-colors">
                      {layout.name}
                    </h3>
                  </div>

                  <div className="mt-4 pt-3 border-t border-slate-100 flex justify-end">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleViewBlueprint(layout)}
                      disabled={blueprintLoading}
                      className="gap-1.5 text-xs"
                    >
                      <Eye className="w-3.5 h-3.5" />
                      View Blueprint
                    </Button>
                  </div>
                </Card>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Blueprint Preview Modal */}
      {previewLayout && (
        <BlueprintPreviewModal
          layout={previewLayout}
          onClose={() => setPreviewLayout(null)}
        />
      )}
    </div>
  )
}

function BlueprintPreviewModal({ layout, onClose }) {
  const seatColorMap = {
    Standard: 'bg-white border-slate-300 text-slate-700',
    Window: 'bg-sky-50 border-sky-300 text-sky-800',
    Aisle: 'bg-slate-100 border-slate-200 text-slate-500',
    VIP: 'bg-amber-100 border-amber-300 text-amber-900 font-bold',
  }

  const seats = layout?.seats || []
  const totalRows = layout?.totalRows || 1
  const totalColumns = layout?.totalColumns || 1

  const minRow = seats.length > 0 ? Math.min(...seats.map(s => s.rowIndex)) : 0
  const minCol = seats.length > 0 ? Math.min(...seats.map(s => s.columnIndex)) : 0
  const rowOffset = minRow === 1 ? 1 : 0
  const colOffset = minCol === 1 ? 1 : 0

  const grid = Array.from({ length: totalRows }, () =>
    Array.from({ length: totalColumns }, () => null)
  )

  seats.forEach((seat) => {
    const r = seat.rowIndex - rowOffset
    const c = seat.columnIndex - colOffset
    if (r >= 0 && r < totalRows && c >= 0 && c < totalColumns) {
      grid[r][c] = seat
    }
  })

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="w-full max-w-xl bg-white border border-slate-200 rounded-2xl p-6 shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-slate-100 pb-3 mb-4">
          <div>
            <h3 className="text-base font-bold text-slate-900 font-display">
              {layout.name}
            </h3>
            <span className="text-xs text-slate-500 font-mono">
              {totalRows} Rows × {totalColumns} Cols • {seats.length || layout.totalSeats || 0} Total Seats
            </span>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-lg text-slate-400 hover:text-slate-600"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="text-center mb-3">
          <div className="inline-block px-3 py-1 rounded-full bg-slate-100 text-[10px] text-slate-700 uppercase tracking-wider font-semibold font-mono border border-slate-200">
            🚍 Front Driver Cabin
          </div>
        </div>

        <div className="p-4 bg-slate-50 rounded-xl border border-slate-200 flex flex-col items-center gap-1.5 max-h-96 overflow-y-auto">
          {grid.map((row, rowIdx) => (
            <div key={rowIdx} className="flex items-center gap-1.5">
              <span className="text-[10px] text-slate-400 w-5 text-right font-mono font-bold">{rowIdx + 1}</span>
              {row.map((seat, colIdx) => (
                <React.Fragment key={colIdx}>
                  {seat ? (
                    <div
                      className={`h-10 w-10 rounded-lg border text-xs font-mono font-bold flex items-center justify-center shadow-xs ${
                        seatColorMap[seat.seatClass] || seatColorMap.Standard
                      }`}
                      title={`${seat.seatNumber} (${seat.seatClass || 'Standard'})`}
                    >
                      {seat.seatNumber}
                    </div>
                  ) : (
                    <div className="h-10 w-10 rounded-lg border border-dashed border-slate-200 bg-slate-100/50" />
                  )}
                </React.Fragment>
              ))}
            </div>
          ))}
        </div>

        <div className="flex justify-end pt-4 border-t border-slate-100 mt-4">
          <Button variant="outline" size="sm" onClick={onClose}>
            Close Blueprint
          </Button>
        </div>
      </div>
    </div>
  )
}
