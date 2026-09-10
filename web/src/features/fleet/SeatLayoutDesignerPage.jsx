import React, { useState, useEffect } from 'react'
import { fleetApi } from './fleetApi'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import {
  LayoutGrid, Plus, X, Save, Trash2,
  CheckCircle2, AlertCircle, Loader2, RotateCcw, Eye
} from 'lucide-react'

const SEAT_TYPES = [
  { key: 'empty', label: 'Empty', color: 'border-dashed border-slate-800 bg-transparent' },
  { key: 'Standard', label: 'Standard', color: 'bg-waypoint-blue/80 border-waypoint-blue text-white' },
  { key: 'Window', label: 'Window', color: 'bg-sky-600/80 border-sky-500 text-white' },
  { key: 'VIP', label: 'VIP', color: 'bg-purple-600/80 border-purple-500 text-white' },
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

  // Preview state
  const [previewLayout, setPreviewLayout] = useState(null)

  const fetchLayouts = async () => {
    setLoading(true)
    try {
      const data = await fleetApi.getSeatLayouts()
      setLayouts(data)
    } catch (err) {
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
  }, [rows, cols]) // eslint-disable-line react-hooks/exhaustive-deps

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
      grid.forEach((row, rowIdx) => {
        row.forEach((cell, colIdx) => {
          if (cell.type !== 'empty') {
            seats.push({
              seatNumber: cell.seatNumber,
              rowIndex: rowIdx,
              columnIndex: colIdx,
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

      setMessage({ type: 'success', text: `Layout "${layoutName}" created with ${totalSeats} seats!` })
      setShowDesigner(false)
      fetchLayouts()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.detail || 'Failed to create layout' })
    } finally {
      setSaving(false)
    }
    setTimeout(() => setMessage(null), 4000)
  }

  const handlePreview = async (layoutId) => {
    try {
      const data = await fleetApi.getSeatLayoutById(layoutId)
      setPreviewLayout(data)
    } catch {
      setMessage({ type: 'error', text: 'Failed to load layout preview' })
    }
  }

  const seatTypeColor = (type) => {
    return SEAT_TYPES.find(t => t.key === type)?.color || SEAT_TYPES[0].color
  }

  return (
    <div className="space-y-5">
      {/* Message Toast */}
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

      {/* Existing Layouts Grid */}
      {!showDesigner && (
        <Card>
          <CardHeader
            title="Seat Layout Templates"
            subtitle="Visual bus seat configurations for fleet assignment"
            action={
              <Button size="sm" onClick={handleStartDesigner}>
                <Plus className="w-4 h-4 mr-1.5" /> New Layout
              </Button>
            }
          />

          {loading ? (
            <div className="text-center py-12">
              <Loader2 className="w-6 h-6 animate-spin text-waypoint-blue mx-auto" />
            </div>
          ) : layouts.length === 0 ? (
            <div className="text-center py-12">
              <LayoutGrid className="w-8 h-8 text-slate-700 mx-auto mb-2" />
              <p className="text-sm text-slate-500">No seat layouts defined yet</p>
              <p className="text-xs text-slate-600 mt-1">Create your first layout with the visual designer</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {layouts.map((layout) => (
                <div
                  key={layout.id}
                  className="p-4 rounded-xl bg-slate-950 border border-slate-800 hover:border-slate-700 transition-colors"
                >
                  <div className="flex items-start justify-between mb-3">
                    <div>
                      <h4 className="text-sm font-semibold text-white">{layout.name}</h4>
                      <p className="text-xs text-slate-400 mt-0.5">
                        {layout.totalRows} × {layout.totalColumns} grid
                      </p>
                    </div>
                    <div className="px-2 py-0.5 rounded-full bg-indigo-500/15 text-indigo-300 text-xs font-medium border border-indigo-500/30">
                      {layout.totalSeats} seats
                    </div>
                  </div>

                  {/* Mini preview grid */}
                  <div className="flex flex-col items-center gap-0.5 p-3 bg-slate-900 rounded-lg mb-3">
                    {Array.from({ length: Math.min(layout.totalRows, 5) }, (_, r) => (
                      <div key={r} className="flex gap-0.5">
                        {Array.from({ length: layout.totalColumns }, (_, c) => {
                          const showAisle = layout.totalColumns === 4 && c === 2
                          return (
                            <React.Fragment key={c}>
                              {showAisle && <div className="w-1.5" />}
                              <div className="w-3 h-3 rounded-sm bg-waypoint-blue/60 border border-waypoint-blue/40" />
                            </React.Fragment>
                          )
                        })}
                      </div>
                    ))}
                    {layout.totalRows > 5 && (
                      <span className="text-[9px] text-slate-600 mt-0.5">+{layout.totalRows - 5} more rows</span>
                    )}
                  </div>

                  <Button variant="outline" size="sm" className="w-full" onClick={() => handlePreview(layout.id)}>
                    <Eye className="w-3.5 h-3.5 mr-1.5" /> View Full Layout
                  </Button>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {/* Visual Grid Designer */}
      {showDesigner && (
        <Card>
          <CardHeader
            title="Visual Seat Layout Designer"
            subtitle="Click cells to cycle: Standard → Window → VIP → Empty"
            action={
              <Button variant="outline" size="sm" onClick={() => setShowDesigner(false)}>
                <X className="w-4 h-4 mr-1.5" /> Cancel
              </Button>
            }
          />

          {/* Config Bar */}
          <div className="flex items-end gap-4 mb-6 pb-5 border-b border-slate-800">
            <div className="flex-1 max-w-xs">
              <label className="block text-xs font-medium text-slate-400 mb-1.5">Layout Name</label>
              <input
                type="text"
                required
                placeholder="e.g. Standard 2×2 (40 seats)"
                value={layoutName}
                onChange={(e) => setLayoutName(e.target.value)}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
              />
            </div>
            <div className="w-24">
              <label className="block text-xs font-medium text-slate-400 mb-1.5">Rows</label>
              <input
                type="number" min={1} max={20}
                value={rows}
                onChange={(e) => setRows(Math.max(1, Math.min(20, parseInt(e.target.value) || 1)))}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
              />
            </div>
            <div className="w-24">
              <label className="block text-xs font-medium text-slate-400 mb-1.5">Columns</label>
              <input
                type="number" min={1} max={6}
                value={cols}
                onChange={(e) => setCols(Math.max(1, Math.min(6, parseInt(e.target.value) || 1)))}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
              />
            </div>
            <Button variant="ghost" size="sm" onClick={initializeGrid} title="Reset grid">
              <RotateCcw className="w-4 h-4 mr-1.5" /> Reset
            </Button>
          </div>

          {/* Interactive Grid */}
          <div className="flex gap-6">
            <div className="flex-1">
              {/* Bus Front */}
              <div className="text-center mb-3">
                <div className="inline-block px-4 py-1 rounded-t-xl bg-slate-800 text-[10px] text-slate-400 uppercase tracking-wider font-medium">
                  🚌 Front
                </div>
              </div>

              <div className="flex flex-col items-center gap-2 p-5 bg-slate-950 rounded-xl border border-slate-800">
                {grid.map((row, rowIdx) => (
                  <div key={rowIdx} className="flex items-center gap-2">
                    <span className="text-[10px] text-slate-600 w-5 text-right font-mono">{rowIdx + 1}</span>
                    {row.map((cell, colIdx) => {
                      const showAisle = cols === 4 && colIdx === 2
                      return (
                        <React.Fragment key={colIdx}>
                          {showAisle && <div className="w-5" />}
                          <button
                            onClick={() => toggleCell(rowIdx, colIdx)}
                            className={`w-12 h-12 rounded-lg border-2 flex items-center justify-center text-[10px] font-bold transition-all hover:scale-105 active:scale-95 ${
                              seatTypeColor(cell.type)
                            }`}
                            title={`Row ${rowIdx + 1}, Col ${colIdx + 1} — ${cell.type} (click to change)`}
                          >
                            {cell.type !== 'empty' ? cell.seatNumber : ''}
                          </button>
                        </React.Fragment>
                      )
                    })}
                  </div>
                ))}
              </div>
            </div>

            {/* Sidebar Stats */}
            <div className="w-48 space-y-4">
              <div className="p-4 rounded-xl bg-slate-950 border border-slate-800">
                <div className="text-2xl font-bold font-display text-waypoint-blue">{totalSeats}</div>
                <div className="text-xs text-slate-400">Total Seats</div>
              </div>

              {/* Legend */}
              <div className="p-4 rounded-xl bg-slate-950 border border-slate-800">
                <div className="text-xs font-medium text-slate-400 mb-3">Click to cycle type:</div>
                <div className="space-y-2">
                  {SEAT_TYPES.map((type) => (
                    <div key={type.key} className="flex items-center gap-2">
                      <div className={`w-5 h-5 rounded border-2 ${type.color}`} />
                      <span className="text-xs text-slate-300">{type.label}</span>
                      <span className="text-xs text-slate-600 ml-auto">
                        {grid.flat().filter(c => c.type === type.key).length}
                      </span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="text-[10px] text-slate-600 px-1">
                Grid: {rows} × {cols} = {rows * cols} cells
              </div>
            </div>
          </div>

          {/* Save Bar */}
          <div className="flex items-center justify-end gap-3 mt-6 pt-4 border-t border-slate-800">
            <Button variant="outline" size="sm" onClick={() => setShowDesigner(false)}>
              Cancel
            </Button>
            <Button size="sm" onClick={handleSave} disabled={saving}>
              {saving ? <Loader2 className="w-4 h-4 animate-spin mr-1.5" /> : <Save className="w-4 h-4 mr-1.5" />}
              {saving ? 'Saving...' : `Save Layout (${totalSeats} seats)`}
            </Button>
          </div>
        </Card>
      )}

      {/* Full Layout Preview Modal */}
      {previewLayout && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-lg p-6">
            <div className="flex items-center justify-between mb-5">
              <div>
                <h3 className="text-lg font-semibold font-display text-white">{previewLayout.name}</h3>
                <p className="text-xs text-slate-400">{previewLayout.totalRows} × {previewLayout.totalColumns} • {previewLayout.seats.length} seats</p>
              </div>
              <button onClick={() => setPreviewLayout(null)} className="p-1 text-slate-400 hover:text-white">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="text-center mb-3">
              <div className="inline-block px-4 py-1 rounded-t-xl bg-slate-800 text-[10px] text-slate-400 uppercase tracking-wider font-medium">
                🚌 Front
              </div>
            </div>

            <div className="flex flex-col items-center gap-1.5 p-4 bg-slate-950 rounded-xl border border-slate-800">
              {Array.from({ length: previewLayout.totalRows }, (_, rowIdx) => (
                <div key={rowIdx} className="flex items-center gap-1.5">
                  <span className="text-[10px] text-slate-600 w-5 text-right font-mono">{rowIdx + 1}</span>
                  {Array.from({ length: previewLayout.totalColumns }, (_, colIdx) => {
                    const showAisle = previewLayout.totalColumns === 4 && colIdx === 2
                    const seat = previewLayout.seats.find(s => s.rowIndex === rowIdx && s.columnIndex === colIdx)
                    return (
                      <React.Fragment key={colIdx}>
                        {showAisle && <div className="w-4" />}
                        {seat ? (
                          <div
                            className={`w-10 h-10 rounded-lg border flex items-center justify-center text-[10px] font-bold text-white ${
                              seatTypeColor(seat.seatClass)
                            }`}
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

            <div className="flex justify-end mt-5">
              <Button variant="outline" size="sm" onClick={() => setPreviewLayout(null)}>Close</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
