import React, { useState, useEffect } from 'react'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { TransitBadge } from '../../components/ui/TransitBadge'
import { Modal } from '../../components/ui/Modal'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { Skeleton } from '../../components/ui/Skeleton'
import { downloadCsv } from '../../lib/csvExport'
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
  Eye,
  Download,
  Calculator,
  ShieldAlert,
  Sliders
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
  const [error, setError] = useState(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedRoute, setSelectedRoute] = useState(null)
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [notification, setNotification] = useState(null)

  // Corridor Simulator State (BR-TRANSFER-001)
  const [simDistance, setSimDistance] = useState(115)
  const [simMultiplier, setSimMultiplier] = useState(1.0)
  const [simTransferBuffer, setSimTransferBuffer] = useState(20)
  const [showSimulator, setShowSimulator] = useState(false)

  // New Route Form State
  const [formData, setFormData] = useState({
    routeNumber: '',
    originCity: '',
    destinationCity: '',
    estimatedDurationMinutes: 180,
    stops: [
      { stopName: '', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
      { stopName: '', sequenceOrder: 2, arrivalOffsetMinutes: 180, distanceFromOriginKm: 120 }
    ]
  })
  const [submitting, setSubmitting] = useState(false)

  const fetchRoutes = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await journeyApi.getRoutes()
      setRoutes(data && data.length > 0 ? data : DEFAULT_ROUTES)
    } catch (err) {
      console.warn('Backend unavailable, using default Sri Lankan routes:', err)
      setRoutes(DEFAULT_ROUTES)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchRoutes()
  }, [])

  const filteredRoutes = routes.filter((r) => {
    const q = searchQuery.toLowerCase()
    return (
      r.routeNumber.toLowerCase().includes(q) ||
      r.originCity.toLowerCase().includes(q) ||
      r.destinationCity.toLowerCase().includes(q)
    )
  })

  // Simulated Fare & BR-TRANSFER-001 calculation
  const calculatedFare = Math.round(simDistance * 8.5 * simMultiplier + 150)
  const isTransferBufferValid = simTransferBuffer >= 15

  const handleExportCsv = () => {
    const exportData = filteredRoutes.map((r) => ({
      RouteNumber: r.routeNumber,
      Origin: r.originCity,
      Destination: r.destinationCity,
      DurationMinutes: r.estimatedDurationMinutes,
      StopCount: r.stops?.length || 0,
      Status: r.isActive ? 'Active' : 'Inactive',
    }))
    downloadCsv(exportData, 'waypoint_intercity_routes.csv')
  }

  const handleAddStop = () => {
    const nextSeq = formData.stops.length + 1
    const lastStop = formData.stops[formData.stops.length - 1]
    const nextOffset = (lastStop?.arrivalOffsetMinutes || 0) + 45
    const nextDist = (lastStop?.distanceFromOriginKm || 0) + 30

    setFormData({
      ...formData,
      stops: [
        ...formData.stops,
        { stopName: '', sequenceOrder: nextSeq, arrivalOffsetMinutes: nextOffset, distanceFromOriginKm: nextDist }
      ]
    })
  }

  const handleRemoveStop = (index) => {
    if (formData.stops.length <= 2) return
    const updated = formData.stops.filter((_, i) => i !== index).map((s, idx) => ({
      ...s,
      sequenceOrder: idx + 1
    }))
    setFormData({ ...formData, stops: updated })
  }

  const handleStopChange = (index, field, value) => {
    const updated = [...formData.stops]
    updated[index][field] = field === 'stopName' ? value : Number(value)
    setFormData({ ...formData, stops: updated })
  }

  const handleCreateRoute = async (e) => {
    e.preventDefault()
    setSubmitting(true)
    try {
      const payload = {
        ...formData,
        estimatedDurationMinutes: Number(formData.estimatedDurationMinutes)
      }
      const created = await journeyApi.createRoute(payload)
      setRoutes([created, ...routes])
      setIsAddModalOpen(false)
      setNotification({ type: 'success', text: `Route ${formData.routeNumber} successfully created in directory!` })
      setTimeout(() => setNotification(null), 4000)
      setFormData({
        routeNumber: '',
        originCity: '',
        destinationCity: '',
        estimatedDurationMinutes: 180,
        stops: [
          { stopName: '', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
          { stopName: '', sequenceOrder: 2, arrivalOffsetMinutes: 180, distanceFromOriginKm: 120 }
        ]
      })
    } catch (err) {
      setNotification({ type: 'error', text: err.response?.data?.detail || 'Failed to save route to backend. Please check inputs.' })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Top Action Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="relative flex-1 sm:w-80">
            <Search className="w-4 h-4 text-slate-500 absolute left-3.5 top-3" />
            <input
              type="text"
              placeholder="Filter by route code, city or corridor..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 bg-white border border-slate-200 rounded-xl text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-waypoint-primary focus:border-transparent transition-all shadow-xs"
            />
          </div>

          <button
            type="button"
            onClick={() => setShowSimulator(!showSimulator)}
            className={`flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border transition-all ${
              showSimulator
                ? 'border-waypoint-primary/40 bg-waypoint-primary/10 text-waypoint-primary'
                : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
            }`}
          >
            <Calculator className="w-3.5 h-3.5" />
            <span>Transfer Buffer Simulator</span>
          </button>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleExportCsv}
            disabled={filteredRoutes.length === 0}
            className="gap-1.5"
          >
            <Download className="w-3.5 h-3.5" />
            Export CSV
          </Button>

          <Button
            variant="primary"
            size="sm"
            onClick={() => setIsAddModalOpen(true)}
            className="gap-1.5 font-bold"
          >
            <Plus className="w-4 h-4" />
            Create Intercity Route
          </Button>
        </div>
      </div>

      {/* Corridor & Transfer Buffer Simulator */}
      {showSimulator && (
        <Card className="border-indigo-100 bg-white p-5 shadow-lg animate-in fade-in duration-150">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3 mb-4">
            <div className="flex items-center gap-2">
              <Calculator className="w-5 h-5 text-waypoint-primary" />
              <div>
                <h3 className="text-sm font-bold text-slate-900 font-display">
                  Corridor Fare & Transfer Buffer Feasibility Simulator
                </h3>
                <p className="text-[11px] text-slate-500">
                  Simulate dynamic peak pricing and validate minimum 15-minute passenger connection windows.
                </p>
              </div>
            </div>
            <button
              onClick={() => setShowSimulator(false)}
              className="text-slate-400 hover:text-slate-700 p-1"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Distance from Origin (km)
              </label>
              <input
                type="number"
                min="10"
                max="600"
                value={simDistance}
                onChange={(e) => setSimDistance(Number(e.target.value))}
                className="w-full px-3 py-2 bg-white border border-slate-200 rounded-lg text-xs font-mono text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
              />
              <span className="text-[10px] text-slate-500 mt-1 block">
                Rate: LKR 8.50/km + LKR 150 base fee
              </span>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Peak Demand Multiplier
              </label>
              <select
                value={simMultiplier}
                onChange={(e) => setSimMultiplier(Number(e.target.value))}
                className="w-full px-3 py-2 bg-white border border-slate-200 rounded-lg text-xs text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
              >
                <option value={1.0}>1.0x (Standard Off-Peak Dispatch)</option>
                <option value={1.25}>1.25x (Friday Weekend Evening Rush)</option>
                <option value={1.5}>1.5x (Sinhala & Tamil New Year / Poya Surge)</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Connection Transfer Buffer (Minutes)
              </label>
              <input
                type="number"
                min="0"
                max="120"
                value={simTransferBuffer}
                onChange={(e) => setSimTransferBuffer(Number(e.target.value))}
                className="w-full px-3 py-2 bg-white border border-slate-200 rounded-lg text-xs font-mono text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
              />
              <span className="text-[10px] text-slate-500 mt-1 block">
                Recommended requirement: Minimum 15 minutes
              </span>
            </div>
          </div>

          <div className="mt-5 p-3.5 rounded-xl bg-slate-50 border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <span className="text-xs text-slate-500">Calculated Dynamic Base Fare:</span>
              <div className="text-xl font-bold font-mono text-waypoint-primary">
                LKR {calculatedFare.toLocaleString()}
              </div>
            </div>

            <div className="flex items-center gap-2">
              {isTransferBufferValid ? (
                <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs font-medium">
                  <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                  <span>Compliant: Safe Connection Window ({simTransferBuffer}m)</span>
                </div>
              ) : (
                <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-red-50 border border-red-200 text-red-700 text-xs font-medium">
                  <ShieldAlert className="w-4 h-4 text-red-500" />
                  <span>Warning: Minimum 15-minute transfer buffer required</span>
                </div>
              )}
            </div>
          </div>
        </Card>
      )}

      {/* Notification Toast */}
      {notification && (
        <div
          role="status"
          className={`p-3.5 rounded-xl border text-xs flex items-center gap-2 ${
            notification.type === 'success'
              ? 'bg-emerald-950/60 border-emerald-800 text-emerald-200'
              : 'bg-red-950/60 border-red-800 text-red-200'
          }`}
        >
          {notification.type === 'success' ? (
            <CheckCircle2 className="w-4 h-4 text-emerald-400" />
          ) : (
            <AlertCircle className="w-4 h-4 text-red-400" />
          )}
          <span>{notification.text}</span>
        </div>
      )}

      {/* Routes Grid View */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton variant="card" />
          <Skeleton variant="card" />
          <Skeleton variant="card" />
          <Skeleton variant="card" />
        </div>
      ) : filteredRoutes.length === 0 ? (
        <EmptyState
          title="No routes found"
          description="No intercity corridors match your search filter. Adjust your criteria or create a new route."
          actionLabel="Clear Filter"
          onAction={() => setSearchQuery('')}
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {filteredRoutes.map((route) => {
            const stopCount = route.stops?.length || 0
            const totalDistance = route.stops?.[stopCount - 1]?.distanceFromOriginKm || 0
            const hours = Math.floor(route.estimatedDurationMinutes / 60)
            const mins = route.estimatedDurationMinutes % 60

            return (
              <Card
                key={route.id}
                className="p-5 flex flex-col justify-between hover:border-slate-700 transition-all shadow-md group"
              >
                <div>
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div className="flex items-center gap-2">
                      <span className="px-2.5 py-1 rounded-lg bg-waypoint-primary/10 text-waypoint-primary border border-waypoint-primary/30 font-mono font-bold text-xs">
                        {route.routeNumber}
                      </span>
                      <TransitBadge
                        status={route.isActive ? 'Available' : 'Cancelled'}
                        label={route.isActive ? 'Active Network' : 'Suspended'}
                      />
                    </div>
                    <div className="text-xs text-slate-400 font-mono flex items-center gap-1">
                      <Clock className="w-3.5 h-3.5 text-slate-500" />
                      {hours}h {mins > 0 ? `${mins}m` : ''}
                    </div>
                  </div>

                  <div className="flex items-center gap-2 text-base font-bold text-slate-900 mb-1">
                    <span>{route.originCity}</span>
                    <ArrowRight className="w-4 h-4 text-waypoint-primary flex-shrink-0" />
                    <span>{route.destinationCity}</span>
                  </div>

                  <div className="flex items-center gap-4 text-xs text-slate-500 mt-2">
                    <span className="flex items-center gap-1 font-mono">
                      <Navigation className="w-3.5 h-3.5 text-slate-400" />
                      {totalDistance > 0 ? `${totalDistance} km` : 'Corridor'}
                    </span>
                    <span className="flex items-center gap-1 font-mono">
                      <ListOrdered className="w-3.5 h-3.5 text-slate-400" />
                      {stopCount} Intermediate Stops
                    </span>
                  </div>
                </div>

                <div className="mt-5 pt-3 border-t border-slate-100 flex items-center justify-between">
                  <span className="text-[11px] text-slate-500">
                    Authority: National Transport Commission
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setSelectedRoute(route)}
                    className="gap-1.5 text-xs text-slate-700 hover:text-slate-900"
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

      {/* Stop Inspection Modal / Drawer */}
      {selectedRoute && (
        <Modal
          isOpen={true}
          onClose={() => setSelectedRoute(null)}
          title={`Route ${selectedRoute.routeNumber} Stops`}
          maxWidth="max-w-2xl"
        >
          <div className="space-y-4">
            <div className="text-sm font-bold text-slate-900">
              {selectedRoute.originCity} to {selectedRoute.destinationCity}
            </div>
            <div className="flex items-center justify-between text-xs text-slate-500 border-b border-slate-200 pb-3">
              <span>Estimated Journey: <strong>{selectedRoute.estimatedDurationMinutes} minutes</strong></span>
              <span>Total Stops: <strong>{selectedRoute.stops?.length || 0}</strong></span>
            </div>

            <div className="max-h-96 overflow-y-auto pr-1 space-y-2">
              {selectedRoute.stops && selectedRoute.stops.length > 0 ? (
                selectedRoute.stops
                  .sort((a, b) => a.sequenceOrder - b.sequenceOrder)
                  .map((stop, idx) => (
                    <div
                      key={stop.id || idx}
                      className="flex items-center justify-between p-3 rounded-xl bg-slate-50 border border-slate-200 text-xs"
                    >
                      <div className="flex items-center gap-3">
                        <span className="flex h-6 w-6 items-center justify-center rounded-full bg-white font-mono text-[11px] font-bold text-indigo-600 border border-slate-200 shadow-xs">
                          {stop.sequenceOrder}
                        </span>
                        <div>
                          <div className="font-semibold text-slate-900">{stop.stopName}</div>
                          <div className="text-[10px] text-slate-500 font-mono">
                            Offset: +{stop.arrivalOffsetMinutes}m from departure
                          </div>
                        </div>
                      </div>

                      <div className="text-right font-mono text-[11px] text-slate-600">
                        {stop.distanceFromOriginKm} km
                      </div>
                    </div>
                  ))
              ) : (
                <div className="text-center py-6 text-xs text-slate-500">
                  No intermediate stop records found.
                </div>
              )}
            </div>

            <div className="flex justify-end pt-3 border-t border-slate-100">
              <Button variant="outline" size="sm" onClick={() => setSelectedRoute(null)}>
                Close
              </Button>
            </div>
          </div>
        </Modal>
      )}

      {/* Create Intercity Route Modal */}
      {isAddModalOpen && (
        <Modal
          isOpen={true}
          onClose={() => setIsAddModalOpen(false)}
          title="New Intercity Route"
          maxWidth="max-w-2xl"
        >
          <form onSubmit={handleCreateRoute} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Route Code
                </label>
                <input
                  type="text"
                  required
                  placeholder="e.g. EX-09"
                  value={formData.routeNumber}
                  onChange={(e) => setFormData({ ...formData, routeNumber: e.target.value })}
                  className="w-full px-3 py-2 bg-white border border-slate-200 rounded-lg text-xs font-mono text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Origin City
                </label>
                <input
                  type="text"
                  required
                  placeholder="Colombo"
                  value={formData.originCity}
                  onChange={(e) => setFormData({ ...formData, originCity: e.target.value })}
                  className="w-full px-3 py-2 bg-white border border-slate-200 rounded-lg text-xs text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Destination City
                </label>
                <input
                  type="text"
                  required
                  placeholder="Ella"
                  value={formData.destinationCity}
                  onChange={(e) => setFormData({ ...formData, destinationCity: e.target.value })}
                  className="w-full px-3 py-2 bg-white border border-slate-200 rounded-lg text-xs text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Estimated Total Duration (Minutes)
              </label>
              <input
                type="number"
                required
                min="10"
                value={formData.estimatedDurationMinutes}
                onChange={(e) => setFormData({ ...formData, estimatedDurationMinutes: e.target.value })}
                className="w-full px-3 py-2 bg-white border border-slate-200 rounded-lg text-xs font-mono text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
              />
            </div>

            {/* Intermediate Stop Builder */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-700">
                  Intermediate Stops Sequence
                </span>
                <button
                  type="button"
                  onClick={handleAddStop}
                  className="text-xs text-waypoint-primary hover:underline font-semibold"
                >
                  + Add Stop
                </button>
              </div>

              <div className="space-y-2 max-h-56 overflow-y-auto pr-1">
                {formData.stops.map((stop, idx) => (
                  <div
                    key={idx}
                    className="flex items-center gap-2 p-2.5 rounded-lg bg-slate-50 border border-slate-200 text-xs"
                  >
                    <span className="font-mono text-slate-400 w-5 text-center font-bold">
                      {stop.sequenceOrder}
                    </span>
                    <input
                      type="text"
                      required
                      placeholder="Stop Name"
                      value={stop.stopName}
                      onChange={(e) => handleStopChange(idx, 'stopName', e.target.value)}
                      className="flex-1 px-2.5 py-1.5 bg-white border border-slate-200 rounded text-xs text-slate-900 focus:outline-none focus:ring-1 focus:ring-waypoint-primary"
                    />
                    <input
                      type="number"
                      placeholder="Offset m"
                      value={stop.arrivalOffsetMinutes}
                      onChange={(e) => handleStopChange(idx, 'arrivalOffsetMinutes', e.target.value)}
                      className="w-20 px-2 py-1.5 bg-white border border-slate-200 rounded text-xs font-mono text-slate-900 text-center"
                    />
                    <input
                      type="number"
                      placeholder="km"
                      value={stop.distanceFromOriginKm}
                      onChange={(e) => handleStopChange(idx, 'distanceFromOriginKm', e.target.value)}
                      className="w-16 px-2 py-1.5 bg-white border border-slate-200 rounded text-xs font-mono text-slate-900 text-center"
                    />
                    {formData.stops.length > 2 && (
                      <button
                        type="button"
                        onClick={() => handleRemoveStop(idx)}
                        className="text-slate-400 hover:text-red-500 p-1"
                      >
                        <X className="w-3.5 h-3.5" />
                      </button>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-3 border-t border-slate-100">
              <Button
                variant="outline"
                size="sm"
                type="button"
                onClick={() => setIsAddModalOpen(false)}
              >
                Cancel
              </Button>
              <Button
                variant="primary"
                size="sm"
                type="submit"
                isLoading={submitting}
              >
                Commit Route
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
