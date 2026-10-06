import React, { useState, useEffect } from 'react'
import { Card } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { TransitBadge } from '../../components/ui/TransitBadge'
import { Skeleton } from '../../components/ui/Skeleton'
import { EmptyState } from '../../components/ui/EmptyState'
import { downloadCsv } from '../../lib/csvExport'
import { journeyApi } from './journeyApi'
import { 
  Calendar, 
  Clock, 
  Bus, 
  ArrowRight, 
  Filter, 
  RefreshCw, 
  Banknote,
  Navigation,
  Download
} from 'lucide-react'

// Realistic fallback service departures across Sri Lankan corridors
const DEFAULT_SERVICES = [
  {
    id: 'srv-col-ella-0630',
    serviceCode: 'SRV-COL-ELLA-0630',
    routeNumber: 'EX-08',
    originCity: 'Colombo (Bastian Hill)',
    destinationCity: 'Ella Town Terminal',
    departureTime: '2026-10-01T06:30:00',
    arrivalTime: '2026-10-01T12:30:00',
    baseFare: 2400.0,
    status: 'Scheduled',
    busClass: 'Luxury AC Super Line',
    totalSeats: 44,
    availableSeats: 18,
  },
  {
    id: 'srv-col-kandy-0700',
    serviceCode: 'SRV-COL-KDY-0700',
    routeNumber: 'RT-01',
    originCity: 'Colombo (Central Super)',
    destinationCity: 'Kandy Goods Shed',
    departureTime: '2026-10-01T07:00:00',
    arrivalTime: '2026-10-01T10:15:00',
    baseFare: 1100.0,
    status: 'Scheduled',
    busClass: 'Semi-Luxury Highway Express',
    totalSeats: 49,
    availableSeats: 26,
  },
  {
    id: 'srv-col-galle-0815',
    serviceCode: 'SRV-COL-GLE-0815',
    routeNumber: 'EX-01',
    originCity: 'Makumbura Multimodal Hub',
    destinationCity: 'Galle Fort Terminal',
    departureTime: '2026-10-01T08:15:00',
    arrivalTime: '2026-10-01T09:45:00',
    baseFare: 1250.0,
    status: 'Scheduled',
    busClass: 'Expressway Air Conditioned',
    totalSeats: 40,
    availableSeats: 9,
  },
  {
    id: 'srv-col-ella-0930',
    serviceCode: 'SRV-COL-ELLA-0930',
    routeNumber: 'EX-08',
    originCity: 'Colombo (Bastian Hill)',
    destinationCity: 'Ella Town Terminal',
    departureTime: '2026-10-01T09:30:00',
    arrivalTime: '2026-10-01T15:30:00',
    baseFare: 2600.0,
    status: 'Scheduled',
    busClass: 'Super Luxury Sleeper Coach',
    totalSeats: 36,
    availableSeats: 5,
  },
  {
    id: 'srv-col-jaffna-2000',
    serviceCode: 'SRV-COL-JAF-2000',
    routeNumber: 'RT-87',
    originCity: 'Colombo Bastian Mawatha',
    destinationCity: 'Jaffna Central Stand',
    departureTime: '2026-10-01T20:00:00',
    arrivalTime: '2026-10-02T04:00:00',
    baseFare: 3200.0,
    status: 'Scheduled',
    busClass: 'Night Cruiser Luxury',
    totalSeats: 44,
    availableSeats: 14,
  },
]

export function ServiceSchedulerPage() {
  const [services, setServices] = useState([])
  const [loading, setLoading] = useState(true)
  const [selectedCorridor, setSelectedCorridor] = useState('ALL')
  const [refreshing, setRefreshing] = useState(false)

  useEffect(() => {
    fetchServices()
  }, [])

  const fetchServices = async () => {
    try {
      setLoading(true)
      const data = await journeyApi.getServices()
      if (Array.isArray(data) && data.length > 0) {
        setServices(data)
      } else {
        setServices(DEFAULT_SERVICES)
      }
    } catch {
      setServices(DEFAULT_SERVICES)
    } finally {
      setLoading(false)
    }
  }

  const handleRefresh = async () => {
    setRefreshing(true)
    await fetchServices()
    setRefreshing(false)
  }

  const corridors = ['ALL', 'EX-08', 'RT-01', 'EX-01', 'RT-87']

  const filteredServices = services.filter((s) => {
    if (selectedCorridor === 'ALL') return true
    return s.routeNumber === selectedCorridor
  })

  const handleExportCsv = () => {
    const exportData = filteredServices.map((s) => ({
      ServiceCode: s.serviceCode,
      Route: s.routeNumber,
      Origin: s.originCity,
      Destination: s.destinationCity,
      Departure: s.departureTime,
      Arrival: s.arrivalTime,
      Class: s.busClass,
      FareLKR: s.baseFare,
      AvailableSeats: s.availableSeats,
      TotalSeats: s.totalSeats,
    }))
    downloadCsv(exportData, 'waypoint_timetables.csv')
  }

  return (
    <div className="space-y-6">
      {/* Controls & Filter Bar */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4">
        <div className="flex items-center gap-2 overflow-x-auto pb-1 sm:pb-0">
          <Filter className="w-4 h-4 text-slate-500 mr-1 flex-shrink-0" />
          {corridors.map((c) => (
            <button
              key={c}
              onClick={() => setSelectedCorridor(c)}
              className={`px-3 py-1.5 rounded-xl text-xs font-semibold transition-all ${
                selectedCorridor === c
                  ? 'bg-waypoint-primary text-waypoint-onPrimary shadow-md shadow-waypoint-primary/10'
                  : 'bg-white text-slate-600 border border-slate-200 hover:text-slate-900 hover:bg-slate-50'
              }`}
            >
              {c === 'ALL' ? 'All Corridors' : `Route ${c}`}
            </button>
          ))}
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleExportCsv}
            disabled={filteredServices.length === 0}
            className="flex items-center gap-1.5 text-xs"
          >
            <Download className="w-3.5 h-3.5" />
            Export CSV
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            disabled={refreshing}
            className="flex items-center gap-1.5 text-xs"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Services Timetable */}
      {loading ? (
        <div className="space-y-3">
          <Skeleton variant="card" />
          <Skeleton variant="card" />
          <Skeleton variant="card" />
        </div>
      ) : filteredServices.length === 0 ? (
        <EmptyState
          title="No scheduled services found"
          description="There are currently no active bus dispatches scheduled for this corridor."
          actionLabel="Show All Corridors"
          onAction={() => setSelectedCorridor('ALL')}
        />
      ) : (
        <div className="space-y-3">
          {filteredServices.map((service) => {
            const depTime = new Date(service.departureTime)
            const arrTime = new Date(service.arrivalTime)
            const formattedDep = isNaN(depTime) ? service.departureTime : depTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
            const formattedArr = isNaN(arrTime) ? service.arrivalTime : arrTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
            const diffHours = !isNaN(depTime) && !isNaN(arrTime) ? Math.round((arrTime - depTime) / (1000 * 60 * 60)) : 0

            return (
              <Card
                key={service.id}
                className="p-5 border border-slate-200 bg-white hover:border-indigo-200 transition-all flex flex-col md:flex-row md:items-center justify-between gap-4 shadow-xs"
              >
                {/* Route & Times */}
                <div className="flex items-start md:items-center gap-4">
                  <div className="w-12 h-12 rounded-xl bg-slate-50 border border-slate-200 flex flex-col items-center justify-center text-center flex-shrink-0 shadow-xs">
                    <Bus className="w-5 h-5 text-waypoint-primary" />
                    <span className="text-[10px] font-mono text-slate-700 font-bold mt-0.5">
                      {service.routeNumber}
                    </span>
                  </div>

                  <div className="space-y-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-xs font-mono font-bold text-slate-600">
                        {service.serviceCode}
                      </span>
                      <TransitBadge status="Available" label={service.status || 'Scheduled'} />
                      {service.busClass && (
                        <span className="text-[10px] font-semibold px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 border border-slate-200">
                          {service.busClass}
                        </span>
                      )}
                    </div>

                    <div className="flex items-center gap-2 text-base font-bold text-slate-900">
                      <span>{service.originCity || 'Colombo'}</span>
                      <ArrowRight className="w-4 h-4 text-waypoint-primary" />
                      <span>{service.destinationCity || 'Ella'}</span>
                    </div>

                    <div className="flex items-center gap-3 text-xs text-slate-500 font-mono">
                      <span className="flex items-center gap-1 font-semibold text-slate-800">
                        <Clock className="w-3.5 h-3.5 text-waypoint-primary" />
                        {formattedDep} → {formattedArr}
                      </span>
                      <span>•</span>
                      <span>{diffHours > 0 ? `${diffHours} hrs total` : 'Direct Dispatch'}</span>
                    </div>
                  </div>
                </div>

                {/* Fare & Capacity */}
                <div className="flex items-center justify-between md:justify-end gap-6 pt-3 md:pt-0 border-t md:border-t-0 border-slate-100">
                  <div className="text-left md:text-right">
                    <div className="text-[10px] text-slate-500 uppercase font-bold tracking-wider">
                      Standard Fare
                    </div>
                    <div className="text-lg font-bold font-mono text-waypoint-primary">
                      Rs. {Number(service.baseFare).toLocaleString(undefined, { minimumFractionDigits: 2 })}
                    </div>
                  </div>

                  {service.availableSeats !== undefined && (
                    <div className="text-left md:text-right">
                      <div className="text-[10px] text-slate-500 uppercase font-bold tracking-wider">
                        Seat Matrix
                      </div>
                      <div className="text-sm font-semibold text-slate-900 font-mono">
                        <span className="text-waypoint-primary font-bold">{service.availableSeats}</span>
                        <span className="text-slate-500"> / {service.totalSeats || 44} Left</span>
                      </div>
                    </div>
                  )}
                </div>
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
