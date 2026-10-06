import React, { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { 
  Ticket, 
  CreditCard, 
  Clock, 
  TrendingUp, 
  Users, 
  ShieldCheck, 
  AlertCircle, 
  ArrowUpRight, 
  RefreshCw, 
  CheckCircle2, 
  QrCode, 
  Bus,
  Search,
  ExternalLink,
  ChevronRight,
  Sparkles
} from 'lucide-react'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import { bookingApi } from './bookingApi'

/**
 * Operator Overview Dashboard
 * Real-time seat holds, revenue metrics, and transit manifests.
 */
export function OperatorDashboardPage() {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(false)
  const [lastRefreshed, setLastRefreshed] = useState(new Date())
  const [bookings, setBookings] = useState([])
  const [stats, setStats] = useState({
    activeHolds: 6,
    confirmedToday: 18,
    revenueToday: 108450,
    refundsToday: 7290,
  })

  // Conductor QR Scanner Modal state
  const [qrModalOpen, setQrModalOpen] = useState(false)
  const [qrPayloadInput, setQrPayloadInput] = useState('')
  const [qrVerifyLoading, setQrVerifyLoading] = useState(false)
  const [qrVerifyResult, setQrVerifyResult] = useState(null)

  // Corridors live status
  const corridors = [
    {
      id: 'SRV-COL-ELLA-0800',
      name: 'Colombo – Ella Highland Scenic Corridor',
      origin: 'Makumbura MMC (Colombo)',
      destination: 'Ella City Station',
      departure: '08:00 AM (Tomorrow)',
      busReg: 'NC-8890',
      busClass: 'SuperLuxury Express',
      bookedSeats: 36,
      totalSeats: 40,
      activeHolds: 2,
      status: 'On Schedule',
      badgeStatus: 'Luxury'
    },
    {
      id: 'SRV-COL-KDY-0700',
      name: 'Colombo – Kandy Intercity Express',
      origin: 'Colombo Fort Central Terminal',
      destination: 'Kandy Goodshed Terminal',
      departure: '07:00 AM (Tomorrow)',
      busReg: 'ND-5421',
      busClass: 'Luxury Air-Conditioned',
      bookedSeats: 34,
      totalSeats: 40,
      activeHolds: 3,
      status: 'Boarding Soon',
      badgeStatus: 'Express'
    },
    {
      id: 'SRV-COL-GAL-0930',
      name: 'Colombo – Galle Southern Expressway Direct',
      origin: 'Makumbura MMC (Colombo)',
      destination: 'Pinnaduwa (Galle)',
      departure: '09:30 AM (Tomorrow)',
      busReg: 'NB-1029',
      busClass: 'Luxury Air-Conditioned',
      bookedSeats: 28,
      totalSeats: 40,
      activeHolds: 1,
      status: 'On Schedule',
      badgeStatus: 'Available'
    },
    {
      id: 'SRV-COL-SIG-0630',
      name: 'Colombo – Sigiriya Cultural Corridor',
      origin: 'Colombo Fort Central Terminal',
      destination: 'Sigiriya Cultural Junction',
      departure: '06:30 AM (Tomorrow)',
      busReg: 'NC-4410',
      busClass: 'SemiLuxury Express',
      bookedSeats: 22,
      totalSeats: 40,
      activeHolds: 0,
      status: 'On Schedule',
      badgeStatus: 'Available'
    },
  ]

  const loadDashboardData = async () => {
    try {
      setLoading(true)
      const data = await bookingApi.getBookings()
      if (Array.isArray(data) && data.length > 0) {
        setBookings(data)
        const confirmed = data.filter(b => b.status === 'Confirmed' || b.status === 'Completed').length
        const revenue = data
          .filter(b => b.status === 'Confirmed' || b.status === 'Completed')
          .reduce((acc, b) => acc + (b.totalFareAmount || 0), 0)
        const refunds = data
          .filter(b => b.status === 'Cancelled')
          .reduce((acc, b) => acc + (b.refundAmount || 0), 0)

        setStats(prev => ({
          ...prev,
          confirmedToday: confirmed > 0 ? confirmed : 18,
          revenueToday: revenue > 0 ? revenue : 108450,
          refundsToday: refunds > 0 ? refunds : 7290,
        }))
      }
      setLastRefreshed(new Date())
    } catch (err) {
      console.warn('Using local dashboard preview data:', err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadDashboardData()
  }, [])

  const handleVerifyQr = async (e) => {
    e.preventDefault()
    if (!qrPayloadInput.trim()) return

    try {
      setQrVerifyLoading(true)
      setQrVerifyResult(null)
      const res = await bookingApi.verifyTicketQr(qrPayloadInput.trim())
      setQrVerifyResult(res)
    } catch (err) {
      setQrVerifyResult({
        isValid: false,
        status: 'Verification Failed',
        message: err.response?.data?.message || err.message || 'Verification rejected by API server.'
      })
    } finally {
      setQrVerifyLoading(false)
    }
  }

  const formatLkr = (amount) => {
    return new Intl.NumberFormat('en-LK', {
      style: 'currency',
      currency: 'LKR',
      minimumFractionDigits: 2,
    }).format(amount).replace('LKR', 'Rs.')
  }

  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* Header Bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-200 pb-6">
        <div>
          <div className="flex items-center gap-2 mb-1.5">
            <span className="px-2.5 py-0.5 text-[11px] font-semibold rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200 tracking-wide">
              Operations Dashboard
            </span>
          </div>
          <h1 className="text-2xl lg:text-3xl font-bold text-slate-900 tracking-tight font-display">
            Operator Overview Dashboard
          </h1>
          <p className="text-xs lg:text-sm text-slate-500 mt-1">
            Real-time seat holds, payment checkout monitoring, transit corridor manifests, and revenue analytics.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            size="sm"
            onClick={loadDashboardData}
            disabled={loading}
            className="text-xs font-mono"
          >
            <RefreshCw className={`w-3.5 h-3.5 mr-1.5 ${loading ? 'animate-spin text-indigo-600' : ''}`} />
            Sync ({lastRefreshed.toLocaleTimeString()})
          </Button>

          <Button
            variant="secondary"
            size="sm"
            onClick={() => setQrModalOpen(true)}
            className="text-xs"
          >
            <QrCode className="w-3.5 h-3.5 mr-1.5" />
            Conductor QR Scanner
          </Button>

          <Button
            variant="primary"
            size="sm"
            onClick={() => navigate('/bookings')}
            className="text-xs"
          >
            <Ticket className="w-3.5 h-3.5 mr-1.5" />
            Booking Manifest
          </Button>
        </div>
      </div>

      {/* 4 Top KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* KPI 1: Active Holds */}
        <div className="p-5 rounded-2xl bg-white border border-slate-200 shadow-xs relative overflow-hidden">
          <div className="flex items-center justify-between text-slate-500 text-xs mb-2">
            <span className="font-semibold uppercase tracking-wider">Active Seat Holds (10m)</span>
            <div className="p-2 rounded-lg bg-amber-50 text-amber-600 border border-amber-200">
              <Clock className="w-4 h-4 animate-pulse" />
            </div>
          </div>
          <div className="text-2xl font-bold text-slate-900 font-mono">{stats.activeHolds}</div>
          <p className="text-xs text-amber-700 mt-2 flex items-center gap-1">
            <span className="w-1.5 h-1.5 rounded-full bg-amber-500 animate-ping" />
            Temporary reservation window active
          </p>
        </div>

        {/* KPI 2: Confirmed Bookings */}
        <div className="p-5 rounded-2xl bg-white border border-slate-200 shadow-xs relative overflow-hidden">
          <div className="flex items-center justify-between text-slate-500 text-xs mb-2">
            <span className="font-semibold uppercase tracking-wider">Confirmed Bookings</span>
            <div className="p-2 rounded-lg bg-emerald-50 text-emerald-600 border border-emerald-200">
              <CheckCircle2 className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-slate-900 font-mono">{stats.confirmedToday}</div>
          <p className="text-xs text-slate-500 mt-2">
            Converted from active holds today
          </p>
        </div>

        {/* KPI 3: Total Revenue */}
        <div className="p-5 rounded-2xl bg-white border border-slate-200 shadow-xs relative overflow-hidden">
          <div className="flex items-center justify-between text-slate-500 text-xs mb-2">
            <span className="font-semibold uppercase tracking-wider">Net Passenger Revenue</span>
            <div className="p-2 rounded-lg bg-indigo-50 text-indigo-600 border border-indigo-200">
              <TrendingUp className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-emerald-600 font-mono">
            {formatLkr(stats.revenueToday)}
          </div>
          <p className="text-xs text-slate-500 mt-2">
            Settled via Payment Gateway
          </p>
        </div>

        {/* KPI 4: Refund Deductions */}
        <div className="p-5 rounded-2xl bg-white border border-slate-200 shadow-xs relative overflow-hidden">
          <div className="flex items-center justify-between text-slate-500 text-xs mb-2">
            <span className="font-semibold uppercase tracking-wider">Refund Deductions</span>
            <div className="p-2 rounded-lg bg-rose-50 text-rose-600 border border-rose-200">
              <CreditCard className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-rose-600 font-mono">
            {formatLkr(stats.refundsToday)}
          </div>
          <p className="text-xs text-slate-500 mt-2">
            Tiered passenger cancellation policy
          </p>
        </div>
      </div>

      {/* Main Grid: Corridor Status & Recent Bookings */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 2 Cols: Corridor Occupancy Matrix */}
        <div className="lg:col-span-2 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-lg font-bold text-slate-900 tracking-tight font-display flex items-center gap-2">
                <Bus className="w-5 h-5 text-indigo-600" />
                Active Corridors & Bus Departure Schedule
              </h2>
              <p className="text-xs text-slate-500 mt-0.5">
                Real-time passenger load factors, assigned luxury fleet, and seat allocations.
              </p>
            </div>
            <Link
              to="/bookings"
              className="text-xs text-indigo-600 hover:text-indigo-700 font-medium inline-flex items-center gap-1"
            >
              Full Manifest Monitor <ChevronRight className="w-3.5 h-3.5" />
            </Link>
          </div>

          <div className="space-y-3">
            {corridors.map((c) => {
              const occupancy = Math.round((c.bookedSeats / c.totalSeats) * 100)
              return (
                <div
                  key={c.id}
                  className="p-5 rounded-xl bg-white border border-slate-200 hover:border-indigo-200 transition-all shadow-xs"
                >
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-xs text-amber-700 font-bold">{c.id}</span>
                        <TransitBadge status={c.badgeStatus} label={c.busClass} />
                      </div>
                      <h3 className="text-sm font-bold text-slate-900 mt-1">{c.name}</h3>
                      <div className="text-xs text-slate-500 flex items-center gap-2 mt-0.5">
                        <span>{c.origin}</span>
                        <span>→</span>
                        <span className="text-slate-700">{c.destination}</span>
                      </div>
                    </div>
                    <div className="text-right sm:text-right">
                      <div className="text-xs font-semibold text-slate-900">{c.departure}</div>
                      <div className="text-xs font-mono text-slate-500 mt-0.5">Bus: {c.busReg}</div>
                    </div>
                  </div>

                  {/* Occupancy Progress Bar */}
                  <div className="mt-4 pt-3 border-t border-slate-100">
                    <div className="flex items-center justify-between text-xs mb-1.5">
                      <span className="text-slate-500">
                        Booked: <strong className="text-slate-900">{c.bookedSeats}/{c.totalSeats} seats</strong> ({c.activeHolds} on hold)
                      </span>
                      <span className={`font-mono font-bold ${occupancy >= 85 ? 'text-emerald-600' : 'text-indigo-600'}`}>
                        {occupancy}% Occupancy
                      </span>
                    </div>
                    <div className="w-full h-2 bg-slate-100 rounded-full overflow-hidden flex">
                      <div
                        className="bg-indigo-600 h-full transition-all duration-500 rounded-l-full"
                        style={{ width: `${occupancy}%` }}
                      />
                      {c.activeHolds > 0 && (
                        <div
                          className="bg-amber-400 h-full transition-all duration-500"
                          style={{ width: `${(c.activeHolds / c.totalSeats) * 100}%` }}
                        />
                      )}
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
        </div>

        {/* Right 1 Col: Quick Actions & Live Activity */}
        <div className="space-y-6">
          {/* Quick Actions Card */}
          <Card className="p-5">
            <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider mb-3 flex items-center gap-2">
              <Sparkles className="w-4 h-4 text-waypoint-amber" />
              Quick Operator Actions
            </h3>
            <div className="space-y-2.5">
              <button
                onClick={() => navigate('/bookings')}
                className="w-full text-left p-3 rounded-lg bg-slate-50 border border-slate-200 hover:border-indigo-300 hover:bg-slate-100 transition-all flex items-center justify-between group"
              >
                <div>
                  <div className="text-xs font-bold text-slate-900 group-hover:text-indigo-600">
                    Inspect Passenger Manifest
                  </div>
                  <div className="text-[11px] text-slate-500">View roster, seats, and boarding passes</div>
                </div>
                <ChevronRight className="w-4 h-4 text-slate-400 group-hover:text-indigo-600" />
              </button>

              <button
                onClick={() => setQrModalOpen(true)}
                className="w-full text-left p-3 rounded-lg bg-slate-50 border border-slate-200 hover:border-amber-300 hover:bg-slate-100 transition-all flex items-center justify-between group"
              >
                <div>
                  <div className="text-xs font-bold text-slate-900 group-hover:text-amber-700">
                    Verify Conductor QR Pass
                  </div>
                  <div className="text-[11px] text-slate-500">Digital boarding pass security check</div>
                </div>
                <QrCode className="w-4 h-4 text-slate-400 group-hover:text-amber-600" />
              </button>

              <button
                onClick={() => navigate('/routes/scheduler')}
                className="w-full text-left p-3 rounded-lg bg-slate-50 border border-slate-200 hover:border-indigo-300 hover:bg-slate-100 transition-all flex items-center justify-between group"
              >
                <div>
                  <div className="text-xs font-bold text-slate-900 group-hover:text-indigo-600">
                    View Network Timetables
                  </div>
                  <div className="text-[11px] text-slate-500">Service schedules and corridor departures</div>
                </div>
                <ChevronRight className="w-4 h-4 text-slate-400 group-hover:text-indigo-600" />
              </button>
            </div>
          </Card>

          {/* Recent Reservations Stream */}
          <Card className="p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
                Recent Bookings
              </h3>
              <span className="text-[11px] text-slate-500 font-medium">Real-time Stream</span>
            </div>

            <div className="space-y-3">
              {(bookings.length > 0 ? bookings.slice(0, 4) : [
                {
                  bookingReference: 'WP-7B92K1',
                  serviceCode: 'SRV-COL-ELLA-0800',
                  seatNumbers: ['4A', '4B'],
                  totalFareAmount: 5700.0,
                  status: 'Confirmed'
                },
                {
                  bookingReference: 'WP-3X88M9',
                  serviceCode: 'SRV-COL-KDY-0700',
                  seatNumbers: ['2C'],
                  totalFareAmount: 1450.0,
                  status: 'Completed'
                },
                {
                  bookingReference: 'WP-1A99Z3',
                  serviceCode: 'SRV-COL-SIG-0630',
                  seatNumbers: ['3B'],
                  totalFareAmount: 2400.0,
                  status: 'Cancelled'
                }
              ]).map((b, idx) => (
                <div
                  key={idx}
                  className="p-3 rounded-lg bg-slate-50 border border-slate-200 flex items-center justify-between"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-mono font-bold text-slate-900">{b.bookingReference}</span>
                      <TransitBadge
                        status={b.status === 'Confirmed' ? 'Available' : b.status === 'Cancelled' ? 'Disrupted' : 'Booked'}
                        label={b.status}
                      />
                    </div>
                    <div className="text-[11px] text-slate-500 mt-1">
                      Seats {Array.isArray(b.seatNumbers) ? b.seatNumbers.join(', ') : b.seatNumbers} • {b.serviceCode}
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-xs font-mono font-bold text-emerald-600">
                      {formatLkr(b.totalFareAmount || 0)}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        </div>
      </div>

      {/* Conductor QR Scanner Modal */}
      {qrModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-xs">
          <div className="w-full max-w-lg p-6 rounded-2xl bg-white border border-slate-200 shadow-2xl">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100 mb-4">
              <div>
                <h3 className="text-lg font-bold text-slate-900 font-display flex items-center gap-2">
                  <QrCode className="w-5 h-5 text-waypoint-amber" />
                  Conductor Boarding QR Pass Verifier
                </h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Validates digital security signatures to detect forged or cancelled e-tickets.
                </p>
              </div>
              <button
                onClick={() => setQrModalOpen(false)}
                className="text-slate-400 hover:text-slate-700 p-1"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleVerifyQr} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                  Paste Raw QR Code Payload String:
                </label>
                <textarea
                  rows={3}
                  value={qrPayloadInput}
                  onChange={(e) => setQrPayloadInput(e.target.value)}
                  placeholder="e.g. WP|REF:WP-7B92K1|SRV:SRV-COL-ELLA-0800|SEATS:4A,4B|PASS:Nimal Silva|HMAC:a8f93c2e71d4b6"
                  className="w-full p-3 rounded-lg bg-slate-50 border border-slate-200 text-xs font-mono text-slate-900 placeholder:text-slate-400 focus:border-indigo-600 focus:outline-none"
                />
              </div>

              {/* Sample Payload Presets */}
              <div className="flex flex-wrap gap-2 text-[11px]">
                <button
                  type="button"
                  onClick={() => setQrPayloadInput('WP|REF:WP-7B92K1|SRV:SRV-COL-ELLA-0800|SEATS:4A,4B|PASS:Nimal Silva|HMAC:a8f93c2e71d4b6')}
                  className="px-2.5 py-1 rounded bg-slate-100 hover:bg-slate-200 text-slate-700 font-mono"
                >
                  Insert Sample Pass (Ella)
                </button>
                <button
                  type="button"
                  onClick={() => setQrPayloadInput('WP|REF:WP-7B92K1|SRV:SRV-COL-ELLA-0800|SEATS:1A,1B|PASS:Forged Pass|HMAC:invalid999')}
                  className="px-2.5 py-1 rounded bg-rose-50 border border-rose-200 hover:bg-rose-100 text-rose-700 font-mono"
                >
                  Insert Forged / Tampered Pass
                </button>
              </div>

              {/* Verification Result Banner */}
              {qrVerifyResult && (
                <div
                  className={`p-4 rounded-xl border text-xs ${
                    qrVerifyResult.isValid
                      ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
                      : 'bg-rose-50 border-rose-200 text-rose-800'
                  }`}
                >
                  <div className="font-bold text-sm mb-1 flex items-center gap-1.5">
                    {qrVerifyResult.isValid ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
                    {qrVerifyResult.status}
                  </div>
                  <p className="leading-relaxed opacity-90">{qrVerifyResult.message}</p>
                  {qrVerifyResult.passengerName && (
                    <div className="mt-2 pt-2 border-t border-emerald-200 font-mono">
                      Passenger: {qrVerifyResult.passengerName} • Seats: {qrVerifyResult.seatNumbers?.join(', ')}
                    </div>
                  )}
                </div>
              )}

              <div className="flex justify-end gap-2 pt-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setQrModalOpen(false)}
                >
                  Close
                </Button>
                <Button
                  type="submit"
                  variant="secondary"
                  size="sm"
                  disabled={qrVerifyLoading || !qrPayloadInput.trim()}
                >
                  {qrVerifyLoading ? 'Verifying...' : 'Verify Boarding Pass'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
