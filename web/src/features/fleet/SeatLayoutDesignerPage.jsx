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
  { key: 'empty', label: 'Aisle / Empty', color: 'border-dashed border-slate-800 bg-transparent text-transparent' },
  { key: 'Standard', label: 'Standard', color: 'bg-slate-800/90 border-slate-700 text-slate-200' },
  { key: 'Window', label: 'Window Seat', color: 'bg-sky-600/80 border-sky-400 text-white' },
  { key: 'VIP', label: 'VIP / Luxury', color: 'bg-waypoint-amber/90 border-amber-400 text-slate-950 font-bold' },
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

  const fetchLayouts = async () => {
    setLoading(true)
    try {
      const data = await fleetApi.getSeatLayouts()
      setLayouts(data || [])
    } catch {
      setMessage({ type: 'error', text: 'Failed to load seat layouts' })
    } finally {
      setLoading(false)
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
              ? 'bg-emerald-950/70 border-emerald-800 text-emerald-200'
              : 'bg-red-950/70 border-red-800 text-red-200'
          }`}
        >
          {message.type === 'success' ? <CheckCircle2 className="w-4 h-4 text-emerald-400" /> : <AlertCircle className="w-4 h-4 text-red-400" />}
          <span>{message.text}</span>
        </div>
      )}

      {/* Main Designer Screen */}
      {showDesigner ? (
        <Card className="p-6 border-slate-800 bg-slate-900/90 shadow-2xl">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-800 pb-4 mb-6">
            <div>
              <h2 className="text-xl font-black text-white tracking-tight font-display">
                Visual Seat Layout Designer
              </h2>
              <p className="text-xs text-slate-400 mt-1">
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
                <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                  Layout Template Name
                </label>
                <input
                  type="text"
                  placeholder="e.g. Standard 2×2 (40 seats)"
                  value={layoutName}
                  onChange={(e) => setLayoutName(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                    Rows
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="15"
                    value={rows}
                    onChange={(e) => setRows(Math.max(1, parseInt(e.target.value) || 1))}
                    className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs font-mono text-slate-100 focus:outline-none focus:ring-1 focus:ring-waypoint-primary text-center"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                    Columns
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="6"
                    value={cols}
                    onChange={(e) => setCols(Math.max(1, parseInt(e.target.value) || 1))}
                    className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs font-mono text-slate-100 focus:outline-none focus:ring-1 focus:ring-waypoint-primary text-center"
                  />
                </div>
              </div>

              {/* Total Seats Counter */}
              <div className="p-4 rounded-xl bg-slate-950 border border-slate-800 shadow-inner">
                <span className="text-xs text-slate-400 font-medium">Total Seats</span>
                <div className="text-3xl font-black font-mono text-waypoint-primary mt-1">
                  {totalSeats}
                </div>
                <div className="text-[11px] text-slate-500 mt-1">
                  Active Passenger Capacity
                </div>
              </div>

              {/* Highlight Filters */}
              <div>
                <span className="block text-[11px] font-bold uppercase tracking-wider text-slate-400 mb-2">
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
                          : 'bg-slate-950 text-slate-400 border border-slate-800 hover:text-white'
                      }`}
                    >
                      {h}
                    </button>
                  ))}
                </div>
              </div>

              {/* Seat Legend */}
              <div className="p-3.5 rounded-xl bg-slate-950 border border-slate-800 space-y-2">
                <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 block mb-1">
                  Seat Tier Legend:
                </span>
                {SEAT_TYPES.map((type) => (
                  <div key={type.key} className="flex items-center gap-2 text-xs">
                    <span className={`w-4 h-4 rounded border text-center text-[10px] flex items-center justify-center ${type.color}`}>
                      {type.key === 'empty' ? '' : '•'}
                    </span>
                    <span className="text-slate-300">{type.label}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Right Bus Grid Canvas */}
            <div className="lg:col-span-3 flex flex-col items-center justify-center p-6 rounded-2xl bg-slate-950 border border-slate-800/80 shadow-inner min-h-[420px]">
              {/* Bus Front Windshield / Driver Indicator */}
              <div className="w-full max-w-sm mb-4 pb-3 border-b-2 border-dashed border-slate-800 flex items-center justify-between text-xs text-slate-500 font-mono">
                <span className="flex items-center gap-1.5 font-bold text-slate-400">
                  <Compass className="w-4 h-4 text-waypoint-primary" /> FRONT / DRIVER CABIN
                </span>
                <span>ENTRY DOOR ➔</span>
              </div>

              <div
                className="grid gap-2 p-4 bg-slate-900/60 rounded-2xl border border-slate-800 shadow-2xl overflow-x-auto max-w-full"
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
                          isHighlighted ? 'opacity-100 ring-2 ring-waypoint-primary/40' : 'opacity-30'
                        } hover:scale-105 active:scale-95`}
                      >
                        {cell.seatNumber}
                      </button>
                    )
                  })
                )}
              </div>

              <div className="w-full max-w-sm mt-4 pt-3 border-t-2 border-dashed border-slate-800 text-center text-xs text-slate-600 font-mono">
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
              <h2 className="text-xl font-bold text-white tracking-tight font-display">
                Seat Layout Templates
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Pre-configured seating matrices mapped to luxury, semi-luxury, and highway express buses.
              </p>
            </div>

            <Button
              variant="primary"
              size="sm"
              onClick={handleStartDesigner}
              className="gap-1.5 font-bold shadow-lg shadow-waypoint-primary/20"
            >
              <Plus className="w-4 h-4" />
              New Layout
            </Button>
          </div>

          {loading ? (
            <div className="flex items-center justify-center p-12 text-slate-400 gap-2">
              <Loader2 className="w-5 h-5 animate-spin text-waypoint-primary" />
              <span>Loading seat layouts...</span>
            </div>
          ) : layouts.length === 0 ? (
            <Card className="p-8 text-center text-slate-400 border border-slate-800 bg-slate-900/60 rounded-2xl">
              <LayoutGrid className="w-8 h-8 text-slate-600 mx-auto mb-3" />
              <p className="text-sm font-semibold text-slate-300">No seat layouts defined yet</p>
              <p className="text-xs text-slate-500 mt-1">Click "New Layout" above to design your first bus seating configuration.</p>
            </Card>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {layouts.map((layout) => (
                <Card
                  key={layout.id}
                  className="p-5 flex flex-col justify-between hover:border-slate-700 transition-all shadow-md group"
                >
                  <div>
                    <div className="flex items-start justify-between gap-3 mb-2">
                      <span className="px-2 py-0.5 rounded-md bg-waypoint-primary/10 text-waypoint-primary border border-waypoint-primary/30 font-mono font-bold text-xs">
                        {layout.totalRows} × {layout.totalColumns} Grid
                      </span>
                      <span className="text-xs font-mono font-bold text-slate-200">
                        {layout.seats?.length || 0} Seats
                      </span>
                    </div>

                    <h3 className="text-base font-bold text-white mb-1 group-hover:text-waypoint-primary transition-colors">
                      {layout.name}
                    </h3>
                  </div>

                  <div className="mt-4 pt-3 border-t border-slate-800 flex justify-end">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setPreviewLayout(layout)}
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
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm"
          onClick={() => setPreviewLayout(null)}
        >
          <div
            className="w-full max-w-xl bg-slate-900 border border-slate-800 rounded-2xl p-6 shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
              <div>
                <h3 className="text-base font-bold text-white font-display">
                  {previewLayout.name}
                </h3>
                <span className="text-xs text-slate-400 font-mono">
                  {previewLayout.totalRows} Rows × {previewLayout.totalColumns} Cols • {previewLayout.seats?.length || 0} Total Seats
                </span>
              </div>
              <button
                type="button"
                onClick={() => setPreviewLayout(null)}
                className="p-1 rounded-lg text-slate-400 hover:text-white"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-4 bg-slate-950 rounded-xl border border-slate-800 flex flex-col items-center max-h-96 overflow-y-auto">
              <div
                className="grid gap-2"
                style={{
                  gridTemplateColumns: `repeat(${previewLayout.totalColumns}, minmax(40px, 48px))`,
                }}
              >
                {previewLayout.seats?.map((seat) => (
                  <div
                    key={seat.seatNumber}
                    className="h-10 w-10 rounded-lg bg-slate-800 border border-slate-700 text-slate-200 text-xs font-mono font-bold flex items-center justify-center"
                  >
                    {seat.seatNumber}
                  </div>
                ))}
              </div>
            </div>

            <div className="flex justify-end pt-4">
              <Button variant="outline" size="sm" onClick={() => setPreviewLayout(null)}>
                Close Blueprint
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
