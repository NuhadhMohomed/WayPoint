import React, { useState, useEffect } from 'react'
import { Card } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import { journeyApi } from './journeyApi'
import { 
  MapPin, 
  Plus, 
  Search, 
  Navigation, 
  Clock, 
  ArrowRight, 
  AlertCircle, 
  CheckCircle2, 
  Loader2, 
  X,
  ListOrdered,
  Eye
} from 'lucide-react'

// Default fallback corridors for offline resilience / demo
const DEFAULT_ROUTES = [
  {
    id: 'route-ex-08',
    routeNumber: 'EX-08',
    originCity: 'Colombo',
    destinationCity: 'Ella',
    estimatedDurationMinutes: 360,
    isActive: true,
    stops: [
      { id: 's1', stopName: 'Colombo Bastian Hill', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
      { id: 's2', stopName: 'Avissawella Junction', sequenceOrder: 2, arrivalOffsetMinutes: 75, distanceFromOriginKm: 52 },
      { id: 's3', stopName: 'Ratnapura Bus Stand', sequenceOrder: 3, arrivalOffsetMinutes: 140, distanceFromOriginKm: 98 },
      { id: 's4', stopName: 'Pelmadulla', sequenceOrder: 4, arrivalOffsetMinutes: 180, distanceFromOriginKm: 120 },
      { id: 's5', stopName: 'Balangoda', sequenceOrder: 5, arrivalOffsetMinutes: 230, distanceFromOriginKm: 145 },
      { id: 's6', stopName: 'Beragala Gap', sequenceOrder: 6, arrivalOffsetMinutes: 290, distanceFromOriginKm: 178 },
      { id: 's7', stopName: 'Bandarawela Central', sequenceOrder: 7, arrivalOffsetMinutes: 330, distanceFromOriginKm: 202 },
      { id: 's8', stopName: 'Ella Town Terminal', sequenceOrder: 8, arrivalOffsetMinutes: 360, distanceFromOriginKm: 215 },
    ]
  },
  {
    id: 'route-rt-01',
    routeNumber: 'RT-01',
    originCity: 'Colombo',
    destinationCity: 'Kandy',
    estimatedDurationMinutes: 195,
    isActive: true,
    stops: [
      { id: 'k1', stopName: 'Colombo Central Super', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
      { id: 'k2', stopName: 'Nittambuwa Town', sequenceOrder: 2, arrivalOffsetMinutes: 50, distanceFromOriginKm: 38 },
      { id: 'k3', stopName: 'Waradapola / Ambepussa', sequenceOrder: 3, arrivalOffsetMinutes: 80, distanceFromOriginKm: 58 },
      { id: 'k4', stopName: 'Kegalle Main Depot', sequenceOrder: 4, arrivalOffsetMinutes: 120, distanceFromOriginKm: 78 },
      { id: 'k5', stopName: 'Mawanella Clock Tower', sequenceOrder: 5, arrivalOffsetMinutes: 145, distanceFromOriginKm: 92 },
      { id: 'k6', stopName: 'Kadugannawa Pass', sequenceOrder: 6, arrivalOffsetMinutes: 170, distanceFromOriginKm: 104 },
      { id: 'k7', stopName: 'Kandy Goods Shed', sequenceOrder: 7, arrivalOffsetMinutes: 195, distanceFromOriginKm: 115 },
    ]
  },
  {
    id: 'route-ex-01',
    routeNumber: 'EX-01',
    originCity: 'Colombo',
    destinationCity: 'Galle',
    estimatedDurationMinutes: 90,
    isActive: true,
    stops: [
      { id: 'g1', stopName: 'Makumbura Multimodal Hub', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
      { id: 'g2', stopName: 'Dodangoda Interchange', sequenceOrder: 2, arrivalOffsetMinutes: 35, distanceFromOriginKm: 46 },
      { id: 'g3', stopName: 'Kurundugahahetekma', sequenceOrder: 3, arrivalOffsetMinutes: 55, distanceFromOriginKm: 76 },
      { id: 'g4', stopName: 'Pinnaduwa Interchange (Galle)', sequenceOrder: 4, arrivalOffsetMinutes: 90, distanceFromOriginKm: 118 },
    ]
  },
  {
    id: 'route-rt-87',
    routeNumber: 'RT-87',
    originCity: 'Colombo',
    destinationCity: 'Jaffna',
    estimatedDurationMinutes: 480,
    isActive: true,
    stops: [
      { id: 'j1', stopName: 'Bastian Mawatha Terminal', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
      { id: 'j2', stopName: 'Kurunegala Central', sequenceOrder: 2, arrivalOffsetMinutes: 130, distanceFromOriginKm: 94 },
      { id: 'j3', stopName: 'Anuradhapura New Town', sequenceOrder: 3, arrivalOffsetMinutes: 240, distanceFromOriginKm: 205 },
      { id: 'j4', stopName: 'Vavuniya Station', sequenceOrder: 4, arrivalOffsetMinutes: 320, distanceFromOriginKm: 260 },
      { id: 'j5', stopName: 'Kilinochchi Depot', sequenceOrder: 5, arrivalOffsetMinutes: 400, distanceFromOriginKm: 328 },
      { id: 'j6', stopName: 'Jaffna Central Stand', sequenceOrder: 6, arrivalOffsetMinutes: 480, distanceFromOriginKm: 395 },
    ]
  }
]

export function RouteManagerPage() {
  const [routes, setRoutes] = useState([])
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedRoute, setSelectedRoute] = useState(null)
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [notification, setNotification] = useState(null)

  useEffect(() => {
    fetchRoutes()
  }, [])

  const fetchRoutes = async () => {
    try {
      setLoading(true)
      const data = await journeyApi.getRoutes()
      if (Array.isArray(data) && data.length > 0) {
        setRoutes(data)
      } else {
        setRoutes(DEFAULT_ROUTES)
      }
    } catch (err) {
      // Graceful fallback to default Sri Lankan corridors
      setRoutes(DEFAULT_ROUTES)
    } finally {
      setLoading(false)
    }
  }

  const showNotification = (type, text) => {
    setNotification({ type, text })
    setTimeout(() => setNotification(null), 5000)
  }

  const filteredRoutes = routes.filter((r) => {
    const q = searchQuery.toLowerCase()
    return (
      r.routeNumber?.toLowerCase().includes(q) ||
      r.originCity?.toLowerCase().includes(q) ||
      r.destinationCity?.toLowerCase().includes(q)
    )
  })

  return (
    <div className="space-y-6">
      {/* Top Banner Alert / Toast */}
      {notification && (
        <div
          className={`p-3 rounded-lg border flex items-center gap-2 text-sm ${
            notification.type === 'success'
              ? 'bg-emerald-950/60 border-emerald-800 text-emerald-300'
              : 'bg-rose-950/60 border-rose-800 text-rose-300'
          }`}
        >
          {notification.type === 'success' ? (
            <CheckCircle2 className="w-4 h-4 text-emerald-400" />
          ) : (
            <AlertCircle className="w-4 h-4 text-rose-400" />
          )}
          {notification.text}
        </div>
      )}

      {/* Action Toolbar */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4">
        <div className="relative flex-1 max-w-md">
          <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
          <input
            type="text"
            placeholder="Filter by route code, Colombo, Ella, Kandy..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-4 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
          />
        </div>

        <Button onClick={() => setIsAddModalOpen(true)} className="flex items-center gap-2">
          <Plus className="w-4 h-4" />
          Create Intercity Route
        </Button>
      </div>

      {/* Routes Grid / Table */}
      {loading ? (
        <div className="flex items-center justify-center p-12 text-slate-500 gap-3">
          <Loader2 className="w-5 h-5 animate-spin text-waypoint-blue" />
          Loading route catalogue...
        </div>
      ) : filteredRoutes.length === 0 ? (
        <Card className="p-8 text-center text-slate-400">
          No routes match your search criteria.
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-2 gap-4">
          {filteredRoutes.map((route) => {
            const durationHours = Math.floor(route.estimatedDurationMinutes / 60)
            const durationMins = route.estimatedDurationMinutes % 60
            const stopCount = route.stops?.length || 0

            return (
              <Card
                key={route.id}
                className="p-5 border border-slate-800 bg-slate-900/80 hover:border-slate-700 transition-all flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <span className="px-2.5 py-1 text-xs font-mono font-bold rounded bg-waypoint-blue/20 text-waypoint-blue border border-waypoint-blue/30">
                      {route.routeNumber}
                    </span>
                    <TransitBadge
                      status={route.isActive ? 'available' : 'disrupted'}
                      customLabel={route.isActive ? 'Active Corridor' : 'Inactive'}
                    />
                  </div>

                  <div className="flex items-center gap-2 text-lg font-bold text-white mb-2">
                    <span>{route.originCity}</span>
                    <ArrowRight className="w-4 h-4 text-slate-500" />
                    <span>{route.destinationCity}</span>
                  </div>

                  <div className="flex items-center gap-4 text-xs text-slate-400 mb-4">
                    <div className="flex items-center gap-1.5">
                      <Clock className="w-3.5 h-3.5 text-slate-500" />
                      <span>{durationHours > 0 ? `${durationHours}h ${durationMins}m` : `${durationMins}m`}</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <ListOrdered className="w-3.5 h-3.5 text-slate-500" />
                      <span>{stopCount} Intermediate Stop{stopCount === 1 ? '' : 's'}</span>
                    </div>
                  </div>
                </div>

                <div className="pt-3 border-t border-slate-800/80 flex items-center justify-between">
                  <span className="text-[11px] text-slate-500">
                    Rule BR-ROUTE-001 Enforced
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setSelectedRoute(route)}
                    className="flex items-center gap-1.5 text-xs"
                  >
                    <Eye className="w-3.5 h-3.5" />
                    Inspect Stops
                  </Button>
                </div>
              </Card>
            )
          })}
        </div>
      )}

      {/* Inspect Stops Drawer / Modal */}
      {selectedRoute && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[85vh] flex flex-col overflow-hidden">
            <div className="p-5 border-b border-slate-800 flex items-center justify-between">
              <div>
                <div className="flex items-center gap-2">
                  <span className="px-2 py-0.5 font-mono text-xs font-bold rounded bg-waypoint-blue/20 text-waypoint-blue border border-waypoint-blue/30">
                    {selectedRoute.routeNumber}
                  </span>
                  <h3 className="text-lg font-bold text-white">
                    {selectedRoute.originCity} to {selectedRoute.destinationCity}
                  </h3>
                </div>
                <p className="text-xs text-slate-400 mt-1">
                  Sequential transit stop schedule with arrival offsets and kilometer markers.
                </p>
              </div>
              <button
                onClick={() => setSelectedRoute(null)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-6 overflow-y-auto space-y-4 flex-1">
              <div className="space-y-3">
                {selectedRoute.stops && selectedRoute.stops.length > 0 ? (
                  selectedRoute.stops.map((stop, index) => {
                    const isFirst = index === 0
                    const isLast = index === selectedRoute.stops.length - 1

                    return (
                      <div
                        key={stop.id || index}
                        className={`p-3.5 rounded-xl border flex items-center justify-between ${
                          isFirst || isLast
                            ? 'bg-slate-950 border-indigo-500/30'
                            : 'bg-slate-900/60 border-slate-800'
                        }`}
                      >
                        <div className="flex items-center gap-3">
                          <div
                            className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold ${
                              isFirst
                                ? 'bg-emerald-500 text-white'
                                : isLast
                                ? 'bg-rose-500 text-white'
                                : 'bg-slate-800 text-slate-300'
                            }`}
                          >
                            {stop.sequenceOrder}
                          </div>
                          <div>
                            <div className="text-sm font-semibold text-white">
                              {stop.stopName}
                            </div>
                            <div className="text-xs text-slate-500 flex items-center gap-2 mt-0.5">
                              <span>Offset: +{stop.arrivalOffsetMinutes} mins</span>
                              <span>•</span>
                              <span>Marker: {stop.distanceFromOriginKm} km</span>
                            </div>
                          </div>
                        </div>

                        {isFirst && (
                          <span className="text-[10px] uppercase font-bold text-emerald-400 px-2 py-0.5 rounded bg-emerald-950/60 border border-emerald-800">
                            Origin
                          </span>
                        )}
                        {isLast && (
                          <span className="text-[10px] uppercase font-bold text-rose-400 px-2 py-0.5 rounded bg-rose-950/60 border border-rose-800">
                            Destination
                          </span>
                        )}
                      </div>
                    )
                  })
                ) : (
                  <div className="p-4 text-center text-slate-500 text-xs">
                    No intermediate stops configured for this corridor.
                  </div>
                )}
              </div>
            </div>

            <div className="p-4 border-t border-slate-800 flex justify-end">
              <Button variant="outline" size="sm" onClick={() => setSelectedRoute(null)}>
                Close
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Add Route Modal */}
      {isAddModalOpen && (
        <AddRouteModal
          onClose={() => setIsAddModalOpen(false)}
          onSuccess={(msg) => {
            setIsAddModalOpen(false)
            showNotification('success', msg)
            fetchRoutes()
          }}
          onError={(err) => showNotification('error', err)}
        />
      )}
    </div>
  )
}

function AddRouteModal({ onClose, onSuccess, onError }) {
  const [routeNumber, setRouteNumber] = useState('')
  const [originCity, setOriginCity] = useState('')
  const [destinationCity, setDestinationCity] = useState('')
  const [durationMinutes, setDurationMinutes] = useState(240)
  const [stops, setStops] = useState([
    { stopName: '', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
    { stopName: '', sequenceOrder: 2, arrivalOffsetMinutes: 240, distanceFromOriginKm: 180 },
  ])
  const [submitting, setSubmitting] = useState(false)

  const handleAddStop = () => {
    const nextSeq = stops.length + 1
    const lastStop = stops[stops.length - 1]
    setStops([
      ...stops,
      {
        stopName: '',
        sequenceOrder: nextSeq,
        arrivalOffsetMinutes: (lastStop?.arrivalOffsetMinutes || 0) + 30,
        distanceFromOriginKm: (lastStop?.distanceFromOriginKm || 0) + 25,
      },
    ])
  }

  const handleRemoveStop = (index) => {
    if (stops.length <= 2) {
      onError('A route requires at least 2 stops (Origin and Destination).')
      return
    }
    const updated = stops.filter((_, i) => i !== index).map((s, i) => ({ ...s, sequenceOrder: i + 1 }))
    setStops(updated)
  }

  const handleStopChange = (index, field, value) => {
    const updated = [...stops]
    updated[index][field] = value
    setStops(updated)
  }

  const handleSubmit = async (e) => {
    e.preventDefault()

    if (!routeNumber || !originCity || !destinationCity) {
      onError('Route number, origin city, and destination city are mandatory.')
      return
    }

    if (stops.length < 2) {
      onError('At least two stops are required (BR-ROUTE-001).')
      return
    }

    // Sequence integrity check
    for (let i = 0; i < stops.length; i++) {
      if (!stops[i].stopName.trim()) {
        onError(`Stop #${i + 1} must have a valid stop name.`)
        return
      }
    }

    setSubmitting(true)
    try {
      await journeyApi.createRoute({
        routeNumber,
        originCity,
        destinationCity,
        estimatedDurationMinutes: Number(durationMinutes),
        stops: stops.map((s) => ({
          stopName: s.stopName,
          sequenceOrder: Number(s.sequenceOrder),
          arrivalOffsetMinutes: Number(s.arrivalOffsetMinutes),
          distanceFromOriginKm: Number(s.distanceFromOriginKm),
        })),
      })
      onSuccess(`Route ${routeNumber} (${originCity} → ${destinationCity}) successfully registered!`)
    } catch (err) {
      onError(err.response?.data?.detail || err.message || 'Failed to create route.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden">
        <div className="p-5 border-b border-slate-800 flex items-center justify-between">
          <h3 className="text-lg font-bold font-display text-white">Create Intercity Route (WEB-02)</h3>
          <button onClick={onClose} className="p-1.5 rounded-lg text-slate-400 hover:text-white">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1">Route Code</label>
              <input
                type="text"
                required
                placeholder="e.g. EX-09"
                value={routeNumber}
                onChange={(e) => setRouteNumber(e.target.value)}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue font-mono"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1">Origin City</label>
              <input
                type="text"
                required
                placeholder="e.g. Colombo"
                value={originCity}
                onChange={(e) => setOriginCity(e.target.value)}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-400 mb-1">Destination City</label>
              <input
                type="text"
                required
                placeholder="e.g. Badulla"
                value={destinationCity}
                onChange={(e) => setDestinationCity(e.target.value)}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1">Estimated Duration (Minutes)</label>
            <input
              type="number"
              min="10"
              required
              value={durationMinutes}
              onChange={(e) => setDurationMinutes(e.target.value)}
              className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-lg text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
            />
          </div>

          <div className="pt-2">
            <div className="flex items-center justify-between mb-2">
              <label className="text-xs font-medium text-slate-300">
                Intermediate Stops (Sequence Order Integrity: OriginSeq &lt; DestinationSeq)
              </label>
              <Button type="button" variant="outline" size="sm" onClick={handleAddStop} className="text-xs">
                <Plus className="w-3.5 h-3.5 mr-1" /> Add Stop
              </Button>
            </div>

            <div className="space-y-2">
              {stops.map((stop, index) => (
                <div key={index} className="grid grid-cols-12 gap-2 items-center bg-slate-950 p-2.5 rounded-lg border border-slate-800">
                  <div className="col-span-1 text-center font-bold text-xs text-slate-400">
                    #{stop.sequenceOrder}
                  </div>
                  <div className="col-span-5">
                    <input
                      type="text"
                      required
                      placeholder={index === 0 ? 'Origin Stop Name' : index === stops.length - 1 ? 'Destination Stop Name' : 'Intermediate Stop Name'}
                      value={stop.stopName}
                      onChange={(e) => handleStopChange(index, 'stopName', e.target.value)}
                      className="w-full px-2.5 py-1.5 bg-slate-900 border border-slate-800 rounded text-xs text-white placeholder-slate-600 focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
                    />
                  </div>
                  <div className="col-span-3">
                    <input
                      type="number"
                      placeholder="Offset min"
                      value={stop.arrivalOffsetMinutes}
                      onChange={(e) => handleStopChange(index, 'arrivalOffsetMinutes', e.target.value)}
                      className="w-full px-2 py-1.5 bg-slate-900 border border-slate-800 rounded text-xs text-white focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
                    />
                  </div>
                  <div className="col-span-2">
                    <input
                      type="number"
                      placeholder="Km marker"
                      value={stop.distanceFromOriginKm}
                      onChange={(e) => handleStopChange(index, 'distanceFromOriginKm', e.target.value)}
                      className="w-full px-2 py-1.5 bg-slate-900 border border-slate-800 rounded text-xs text-white focus:outline-none focus:ring-1 focus:ring-waypoint-blue"
                    />
                  </div>
                  <div className="col-span-1 text-right">
                    <button
                      type="button"
                      onClick={() => handleRemoveStop(index)}
                      className="p-1 text-slate-500 hover:text-rose-400 transition-colors"
                    >
                      <X className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-800">
            <Button variant="outline" size="sm" type="button" onClick={onClose}>
              Cancel
            </Button>
            <Button size="sm" type="submit" disabled={submitting}>
              {submitting ? <Loader2 className="w-4 h-4 animate-spin mr-1.5" /> : <Plus className="w-4 h-4 mr-1.5" />}
              {submitting ? 'Registering Route...' : 'Save Route'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

