import React, { useState } from 'react'
import { fleetApi } from './fleetApi'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import {
  ClipboardList, Search, Loader2, Users, CheckCircle2,
  Clock, AlertCircle, Armchair
} from 'lucide-react'

export function BookingManifestMonitorPage() {
  const [serviceId, setServiceId] = useState('')
  const [seatMatrix, setSeatMatrix] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  const handleLoadManifest = async (e) => {
    e.preventDefault()
    if (!serviceId.trim()) return

    setLoading(true)
    setError(null)
    setSeatMatrix(null)

    try {
      const data = await fleetApi.getServiceSeatMatrix(serviceId.trim())
      setSeatMatrix(data)
    } catch (err) {
      setError(err.response?.data?.detail || 'Failed to load service data. Check the Service ID.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div>
        <div className="flex items-center gap-2 mb-1">
          <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-sky-500/20 text-sky-300 border border-sky-500/30">
            WEB-11
          </span>
          <span className="text-xs text-slate-400">Shared with Mithila (Component 3)</span>
        </div>
        <h1 className="text-2xl font-bold font-display text-white tracking-tight">
          Booking Manifest & Seat Monitor
        </h1>
        <p className="text-xs text-slate-400 mt-1">
          Real-time seat occupancy dashboard for service departures.
        </p>
      </div>

      {/* Service Selector */}
      <Card>
        <form onSubmit={handleLoadManifest} className="flex items-end gap-3">
          <div className="flex-1 max-w-md">
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Service ID</label>
            <input
              type="text"
              placeholder="Enter Service UUID or select from routes..."
              value={serviceId}
              onChange={(e) => setServiceId(e.target.value)}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            />
          </div>
          <Button size="sm" type="submit" disabled={loading || !serviceId.trim()}>
            {loading ? <Loader2 className="w-4 h-4 animate-spin mr-1.5" /> : <Search className="w-4 h-4 mr-1.5" />}
            {loading ? 'Loading...' : 'Load Manifest'}
          </Button>
        </form>
      </Card>

      {/* Error */}
      {error && (
        <div className="flex items-center gap-2 px-4 py-2.5 rounded-lg text-sm border bg-rose-950/60 border-rose-800 text-rose-300">
          <AlertCircle className="w-4 h-4" /> {error}
        </div>
      )}

      {/* Seat Matrix Results */}
      {seatMatrix && (
        <>
          {/* Stats Cards */}
          <div className="grid grid-cols-4 gap-4">
            <StatCard label="Total Seats" value={seatMatrix.totalSeats} icon={Armchair} color="text-slate-300" />
            <StatCard label="Available" value={seatMatrix.availableSeats} icon={CheckCircle2} color="text-emerald-400" />
            <StatCard label="Held" value={seatMatrix.heldSeats} icon={Clock} color="text-amber-400" />
            <StatCard label="Booked" value={seatMatrix.bookedSeats} icon={Users} color="text-waypoint-blue" />
          </div>

          {/* Occupancy Bar */}
          <Card>
            <CardHeader
              title={`Service ${seatMatrix.serviceCode}`}
              subtitle="Seat occupancy breakdown"
            />
            <div className="h-6 rounded-full bg-slate-950 border border-slate-800 flex overflow-hidden">
              {seatMatrix.bookedSeats > 0 && (
                <div
                  className="bg-waypoint-blue flex items-center justify-center text-[10px] text-white font-bold"
                  style={{ width: `${(seatMatrix.bookedSeats / seatMatrix.totalSeats) * 100}%` }}
                >
                  {seatMatrix.bookedSeats}
                </div>
              )}
              {seatMatrix.heldSeats > 0 && (
                <div
                  className="bg-amber-500 flex items-center justify-center text-[10px] text-white font-bold"
                  style={{ width: `${(seatMatrix.heldSeats / seatMatrix.totalSeats) * 100}%` }}
                >
                  {seatMatrix.heldSeats}
                </div>
              )}
              {seatMatrix.availableSeats > 0 && (
                <div
                  className="bg-emerald-500/30 flex items-center justify-center text-[10px] text-emerald-300 font-bold"
                  style={{ width: `${(seatMatrix.availableSeats / seatMatrix.totalSeats) * 100}%` }}
                >
                  {seatMatrix.availableSeats}
                </div>
              )}
            </div>
            <div className="flex items-center gap-4 mt-3">
              <div className="flex items-center gap-1.5 text-xs text-slate-400">
                <div className="w-3 h-3 rounded-sm bg-waypoint-blue" /> Booked
              </div>
              <div className="flex items-center gap-1.5 text-xs text-slate-400">
                <div className="w-3 h-3 rounded-sm bg-amber-500" /> Held
              </div>
              <div className="flex items-center gap-1.5 text-xs text-slate-400">
                <div className="w-3 h-3 rounded-sm bg-emerald-500/30 border border-emerald-500/40" /> Available
              </div>
            </div>
          </Card>

          {/* Seat Map Visual */}
          <Card>
            <CardHeader title="Live Seat Map" subtitle="Real-time view of seat availability (BR-SEAT-001)" />

            {seatMatrix.seats.length > 0 ? (
              <>
                <div className="text-center mb-3">
                  <div className="inline-block px-4 py-1 rounded-t-xl bg-slate-800 text-[10px] text-slate-400 uppercase tracking-wider font-medium">
                    🚌 Front
                  </div>
                </div>

                <SeatMapGrid seats={seatMatrix.seats} />
              </>
            ) : (
              <div className="text-center py-8">
                <Armchair className="w-8 h-8 text-slate-700 mx-auto mb-2" />
                <p className="text-sm text-slate-500">No seat data available for this service</p>
              </div>
            )}
          </Card>

          {/* Passenger Manifest Table */}
          <Card>
            <CardHeader
              title="Booked Seat Details"
              subtitle={`${seatMatrix.bookedSeats} seats booked across this service`}
            />
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-xs text-slate-400 uppercase tracking-wider border-b border-slate-800">
                    <th className="text-left py-3 px-3 font-medium">Seat #</th>
                    <th className="text-left py-3 px-3 font-medium">Class</th>
                    <th className="text-left py-3 px-3 font-medium">Position</th>
                    <th className="text-center py-3 px-3 font-medium">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {seatMatrix.seats
                    .filter(s => s.status !== 'Available')
                    .sort((a, b) => a.seatNumber.localeCompare(b.seatNumber))
                    .map((seat) => (
                      <tr key={seat.id} className="border-b border-slate-800/50">
                        <td className="py-2.5 px-3 font-mono font-semibold text-white">{seat.seatNumber}</td>
                        <td className="py-2.5 px-3 text-xs text-slate-400">{seat.seatClass}</td>
                        <td className="py-2.5 px-3 text-xs text-slate-500">Row {seat.rowIndex + 1}, Col {seat.columnIndex + 1}</td>
                        <td className="py-2.5 px-3 text-center">
                          <span className={`inline-flex px-2 py-0.5 text-xs font-medium rounded border ${
                            seat.status === 'Booked'
                              ? 'bg-waypoint-blue/20 text-blue-300 border-blue-500/30'
                              : seat.status === 'Held'
                              ? 'bg-amber-500/20 text-amber-300 border-amber-500/30'
                              : 'bg-slate-500/20 text-slate-300 border-slate-500/30'
                          }`}>
                            {seat.status}
                          </span>
                        </td>
                      </tr>
                    ))
                  }
                  {seatMatrix.seats.filter(s => s.status !== 'Available').length === 0 && (
                    <tr>
                      <td colSpan={4} className="text-center py-8 text-sm text-slate-500">
                        All seats are currently available
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </Card>
        </>
      )}
    </div>
  )
}

// ─── Stat Card ───

function StatCard({ label, value, icon: Icon, color }) {
  return (
    <div className="p-4 rounded-xl bg-slate-950 border border-slate-800">
      <div className="flex items-center gap-2 mb-1">
        <Icon className={`w-4 h-4 ${color}`} />
        <span className="text-xs text-slate-400">{label}</span>
      </div>
      <div className={`text-2xl font-bold font-display ${color}`}>{value}</div>
    </div>
  )
}

// ─── Seat Map Grid ───

function SeatMapGrid({ seats }) {
  const maxRow = Math.max(...seats.map(s => s.rowIndex))
  const maxCol = Math.max(...seats.map(s => s.columnIndex))

  const seatStatusColor = {
    Available: 'bg-emerald-500/20 border-emerald-500/40 text-emerald-300',
    Held: 'bg-amber-500/30 border-amber-500/50 text-amber-200',
    Booked: 'bg-waypoint-blue/80 border-waypoint-blue text-white',
    Blocked: 'bg-slate-800 border-slate-700 text-slate-500',
  }

  const seatMap = {}
  seats.forEach((s) => {
    seatMap[`${s.rowIndex}-${s.columnIndex}`] = s
  })

  const totalCols = maxCol + 1

  return (
    <div className="flex flex-col items-center gap-1.5 p-4 bg-slate-950 rounded-xl border border-slate-800">
      {Array.from({ length: maxRow + 1 }, (_, rowIdx) => (
        <div key={rowIdx} className="flex items-center gap-1.5">
          <span className="text-[10px] text-slate-600 w-5 text-right font-mono">{rowIdx + 1}</span>
          {Array.from({ length: totalCols }, (_, colIdx) => {
            const showAisle = totalCols === 4 && colIdx === 2
            const seat = seatMap[`${rowIdx}-${colIdx}`]
            return (
              <React.Fragment key={colIdx}>
                {showAisle && <div className="w-4" />}
                {seat ? (
                  <div
                    className={`w-10 h-10 rounded-lg border flex items-center justify-center text-[10px] font-bold cursor-default transition-all ${
                      seatStatusColor[seat.status] || seatStatusColor.Available
                    }`}
                    title={`${seat.seatNumber} — ${seat.status} (${seat.seatClass})`}
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
  )
}
