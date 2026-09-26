import React, { useState, useEffect } from 'react'
import { 
  Ticket, 
  CreditCard, 
  Clock, 
  Users, 
  ShieldCheck, 
  AlertCircle, 
  RefreshCw, 
  CheckCircle2, 
  QrCode, 
  Bus,
  Search,
  ChevronDown,
  XCircle,
  FileSpreadsheet,
  AlertTriangle,
  Play,
  RotateCcw
} from 'lucide-react'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import { bookingApi } from './bookingApi'

/**
 * WEB-11: Booking Manifest & Payment Sandbox Monitor
 * Stitch Screen ID: 2828cbc93fdf4d3db64e00a5fd242cd3
 * Component 3: Booking, Ticketing & Passenger Options (Mithila)
 */
export function BookingManifestMonitorPage() {
  const [selectedServiceId, setSelectedServiceId] = useState('SRV-COL-ELLA-0800')
  const [searchQuery, setSearchQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState('All')
  const [loading, setLoading] = useState(false)
  const [bookings, setBookings] = useState([])

  // Sandbox Payment Simulator state
  const [sandboxCardPreset, setSandboxCardPreset] = useState('success')
  const [sandboxCardNumber, setSandboxCardNumber] = useState('4000 0000 0000 0001')
  const [sandboxAmount, setSandboxAmount] = useState('5700')
  const [sandboxCardholder, setSandboxCardholder] = useState('NIMAL SILVA')
  const [sandboxLoading, setSandboxLoading] = useState(false)
  const [sandboxResult, setSandboxResult] = useState(null)

  // Seat Hold Simulator state
  const [holdSeatsInput, setHoldSeatsInput] = useState('4C, 4D')
  const [holdLoading, setHoldLoading] = useState(false)
  const [holdResult, setHoldResult] = useState(null)

  // Cancel & Refund Modal state
  const [cancelModalBooking, setCancelModalBooking] = useState(null)
  const [cancelReason, setCancelReason] = useState('Change of travel plans')
  const [cancelLoading, setCancelLoading] = useState(false)

  // Ticket Inspection Modal state
  const [inspectTicket, setInspectTicket] = useState(null)

  const services = [
    {
      id: 'SRV-COL-ELLA-0800',
      title: 'Colombo – Ella Highland Scenic Corridor',
      time: '08:00 AM (Tomorrow)',
      busReg: 'NC-8890',
      busClass: 'SuperLuxury Express',
      farePerSeat: 2850.0,
      totalSeats: 40,
    },
    {
      id: 'SRV-COL-KDY-0700',
      title: 'Colombo – Kandy Intercity Express',
      time: '07:00 AM (Tomorrow)',
      busReg: 'ND-5421',
      busClass: 'Luxury Air-Conditioned',
      farePerSeat: 1450.0,
      totalSeats: 40,
    },
    {
      id: 'SRV-COL-GAL-0930',
      title: 'Colombo – Galle Southern Expressway Direct',
      time: '09:30 AM (Tomorrow)',
      busReg: 'NB-1029',
      busClass: 'Luxury Air-Conditioned',
      farePerSeat: 1150.0,
      totalSeats: 40,
    },
  ]

  const activeService = services.find(s => s.id === selectedServiceId) || services[0]

  // Default manifest passengers
  const defaultManifest = [
    {
      bookingId: 'b1',
      bookingReference: 'WP-7B92K1',
      passengerName: 'Nimal Silva',
      passengerPhone: '+94 77 456 7890',
      seatNumbers: ['4A', '4B'],
      boardingPoint: 'Platform 3, Makumbura MMC (Colombo)',
      totalFareAmount: 5700.0,
      status: 'Confirmed',
      hoursUntilDeparture: 26,
      bookedAt: 'Today, 09:15 AM',
      isBoarded: false,
      ticketQrPayload: 'WP|REF:WP-7B92K1|SRV:SRV-COL-ELLA-0800|SEATS:4A,4B|PASS:Nimal Silva|HMAC:a8f93c2e71d4b6'
    },
    {
      bookingId: 'b2',
      bookingReference: 'WP-5D11K8',
      passengerName: 'Kamal Perera',
      passengerPhone: '+94 71 889 2233',
      seatNumbers: ['12C'],
      boardingPoint: 'Bay 2, Makumbura MMC (Colombo)',
      totalFareAmount: 2850.0,
      status: 'Confirmed',
      hoursUntilDeparture: 14,
      bookedAt: 'Today, 10:30 AM',
      isBoarded: false,
      ticketQrPayload: 'WP|REF:WP-5D11K8|SRV:SRV-COL-ELLA-0800|SEATS:12C|PASS:Kamal Perera|HMAC:d2e148a071bc99'
    },
    {
      bookingId: 'b3',
      bookingReference: 'WP-9C44T2',
      passengerName: 'Dilshan Fernando',
      passengerPhone: '+94 76 334 5566',
      seatNumbers: ['1A', '1B'],
      boardingPoint: 'Platform 3, Makumbura MMC (Colombo)',
      totalFareAmount: 5700.0,
      status: 'Boarded',
      hoursUntilDeparture: 26,
      bookedAt: 'Yesterday, 04:00 PM',
      isBoarded: true,
      ticketQrPayload: 'WP|REF:WP-9C44T2|SRV:SRV-COL-ELLA-0800|SEATS:1A,1B|PASS:Dilshan Fernando|HMAC:cc902188fa3412'
    },
    {
      bookingId: 'b4',
      bookingReference: 'WP-1A99Z3',
      passengerName: 'Anura Wickramasinghe',
      passengerPhone: '+94 77 112 3344',
      seatNumbers: ['3B'],
      boardingPoint: 'Platform 3, Makumbura MMC (Colombo)',
      totalFareAmount: 2850.0,
      status: 'Cancelled',
      refundAmount: 2565.0,
      refundPercentage: 0.90,
      cancellationReason: 'Emergency rescheduling',
      hoursUntilDeparture: 26,
      bookedAt: '2 days ago',
      isBoarded: false,
      ticketQrPayload: 'WP|REF:WP-1A99Z3|SRV:SRV-COL-ELLA-0800|SEATS:3B|PASS:Anura Wickramasinghe|HMAC:98fabc1122'
    }
  ]

  const loadManifest = async () => {
    try {
      setLoading(true)
      const data = await bookingApi.getBookings()
      if (Array.isArray(data) && data.length > 0) {
        setBookings(data)
      } else {
        setBookings(defaultManifest)
      }
    } catch (err) {
      console.warn('Backend offline or fallback to default manifest:', err.message)
      setBookings(defaultManifest)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadManifest()
  }, [selectedServiceId])

  // Filtered manifest
  const filteredBookings = bookings.filter(b => {
    const matchesStatus = statusFilter === 'All' || b.status === statusFilter
    const q = searchQuery.toLowerCase()
    const matchesSearch = !q || 
      b.bookingReference?.toLowerCase().includes(q) ||
      b.passengerName?.toLowerCase().includes(q) ||
      b.passengerPhone?.includes(q)
    return matchesStatus && matchesSearch
  })

  // Handle Preset card change
  const handlePresetChange = (preset) => {
    setSandboxCardPreset(preset)
    setSandboxResult(null)
    if (preset === 'success') {
      setSandboxCardNumber('4000 0000 0000 0001')
    } else if (preset === 'declined') {
      setSandboxCardNumber('4000 0000 0000 0002')
    } else if (preset === 'timeout') {
      setSandboxCardNumber('4000 0000 0000 0003')
    }
  }

  // Execute Sandbox Charge
  const handleExecuteSandboxCharge = async (e) => {
    e.preventDefault()
    try {
      setSandboxLoading(true)
      setSandboxResult(null)
      const res = await bookingApi.confirmPayment({
        cardNumber: sandboxCardNumber,
        cardholderName: sandboxCardholder,
        expiryDate: '08/28',
        cvv: '123',
        amount: parseFloat(sandboxAmount) || 5700.0,
      })
      setSandboxResult(res)
    } catch (err) {
      setSandboxResult({
        isSuccess: false,
        gatewayStatus: err.response?.data?.gatewayStatus || 'Error',
        message: err.response?.data?.message || err.message || 'Payment simulation rejected.'
      })
    } finally {
      setSandboxLoading(false)
    }
  }

  // Execute Seat Hold Simulator
  const handleHoldSeats = async (e) => {
    e.preventDefault()
    const seats = holdSeatsInput.split(',').map(s => s.trim()).filter(Boolean)
    if (seats.length === 0) return

    try {
      setHoldLoading(true)
      setHoldResult(null)
      // Call backend API /api/v1/bookings/hold
      const res = await bookingApi.holdSeat({
        serviceId: '00000000-0000-0000-0000-000000000000', // fallback GUID handled by seeder/backend
        seatNumbers: seats,
        passengerName: 'Simulator Operator User',
      })
      setHoldResult({
        success: true,
        data: res,
        message: `Successfully held seats ${seats.join(', ')} for 10 minutes (Hold ID: ${res.holdId?.substring(0, 8)}...).`
      })
    } catch (err) {
      const detail = err.response?.data?.detail || err.response?.data?.message || err.message
      setHoldResult({
        success: false,
        status: err.response?.status,
        message: detail || 'Seat hold conflict detected.'
      })
    } finally {
      setHoldLoading(false)
    }
  }

  // Execute Operator Cancellation under BR-REFUND-001
  const handleConfirmCancellation = async () => {
    if (!cancelModalBooking) return

    try {
      setCancelLoading(true)
      await bookingApi.cancelBooking({
        bookingReference: cancelModalBooking.bookingReference,
        reason: cancelReason,
      })

      // Update state locally
      setBookings(prev => prev.map(b => {
        if (b.bookingReference === cancelModalBooking.bookingReference) {
          const refundPct = cancelModalBooking.hoursUntilDeparture > 24 ? 0.90 : cancelModalBooking.hoursUntilDeparture >= 12 ? 0.50 : 0.0
          return {
            ...b,
            status: 'Cancelled',
            refundAmount: b.totalFareAmount * refundPct,
            refundPercentage: refundPct,
            cancellationReason: cancelReason
          }
        }
        return b
      }))

      setCancelModalBooking(null)
    } catch (err) {
      alert('Cancellation failed: ' + (err.response?.data?.detail || err.message))
    } finally {
      setCancelLoading(false)
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
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-800/80 pb-6">
        <div>
          <div className="flex items-center gap-2 mb-1.5">
            <span className="px-2.5 py-0.5 text-[11px] font-bold rounded-full bg-waypoint-amber/20 text-waypoint-amber border border-waypoint-amber/30 tracking-wide uppercase">
              WEB-11 • Component 3
            </span>
            <span className="text-xs text-slate-400 font-medium">
              Lead: <strong className="text-white">Mithila</strong> (Manifest & Payment Sandbox)
            </span>
          </div>
          <h1 className="text-2xl lg:text-3xl font-bold text-white tracking-tight font-display">
            Booking Manifest & Payment Sandbox Monitor
          </h1>
          <p className="text-xs lg:text-sm text-slate-400 mt-1">
            Real-time passenger roster, seat reservations, payment gateway simulator, and BR-REFUND-001 cancellations.
          </p>
        </div>

        {/* Service Selector Dropdown */}
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 bg-slate-900 px-3 py-2 rounded-xl border border-slate-800">
            <Bus className="w-4 h-4 text-waypoint-blue" />
            <select
              value={selectedServiceId}
              onChange={(e) => setSelectedServiceId(e.target.value)}
              className="bg-transparent text-xs font-semibold text-white focus:outline-none cursor-pointer"
            >
              {services.map(s => (
                <option key={s.id} value={s.id} className="bg-slate-900 text-white">
                  {s.id} • {s.title}
                </option>
              ))}
            </select>
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={loadManifest}
            disabled={loading}
            className="text-xs"
          >
            <RefreshCw className={`w-3.5 h-3.5 mr-1 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Corridor Service Capacity Stats */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
          <div className="text-xs text-slate-400">Total Capacity</div>
          <div className="text-xl font-bold font-mono text-white mt-1">40 Seats</div>
          <div className="text-[11px] text-slate-500 mt-0.5">{activeService.busClass}</div>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
          <div className="text-xs text-slate-400">Confirmed Booked</div>
          <div className="text-xl font-bold font-mono text-emerald-400 mt-1">
            {bookings.filter(b => b.status === 'Confirmed' || b.status === 'Boarded').length * 2} Seats
          </div>
          <div className="text-[11px] text-emerald-500 mt-0.5">Tickets Issued</div>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
          <div className="text-xs text-slate-400">Temporarily Held</div>
          <div className="text-xl font-bold font-mono text-waypoint-amber mt-1">2 Seats</div>
          <div className="text-[11px] text-amber-500/80 mt-0.5">Ticking 10m window</div>
        </div>

        <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
          <div className="text-xs text-slate-400">Base Fare per Seat</div>
          <div className="text-xl font-bold font-mono text-sky-400 mt-1">
            {formatLkr(activeService.farePerSeat)}
          </div>
          <div className="text-[11px] text-slate-500 mt-0.5">Per passenger rate</div>
        </div>
      </div>

      {/* Main Section: Manifest Table */}
      <Card className="p-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
          <div>
            <h3 className="text-base font-bold text-white font-display flex items-center gap-2">
              <FileSpreadsheet className="w-5 h-5 text-waypoint-blue" />
              Passenger Manifest Roster
            </h3>
            <p className="text-xs text-slate-400 mt-0.5">
              Service: <span className="text-amber-400 font-mono font-semibold">{activeService.id}</span> • {activeService.title} ({activeService.time})
            </p>
          </div>

          {/* Search & Status Filters */}
          <div className="flex flex-wrap items-center gap-3">
            <div className="relative">
              <Search className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                type="text"
                placeholder="Search ref, passenger..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-8 pr-3 py-1.5 rounded-lg bg-slate-950 border border-slate-800 text-xs text-white placeholder:text-slate-600 focus:outline-none focus:border-waypoint-blue"
              />
            </div>

            <div className="flex rounded-lg bg-slate-950 p-1 border border-slate-800 text-xs">
              {['All', 'Confirmed', 'Boarded', 'Cancelled'].map(f => (
                <button
                  key={f}
                  onClick={() => setStatusFilter(f)}
                  className={`px-3 py-1 rounded-md transition-all font-medium ${
                    statusFilter === f
                      ? 'bg-waypoint-blue text-white shadow-sm'
                      : 'text-slate-400 hover:text-white'
                  }`}
                >
                  {f}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="border-b border-slate-800 text-slate-400 uppercase tracking-wider font-mono text-[11px]">
              <tr>
                <th className="pb-3 pl-2">Ref Code</th>
                <th className="pb-3">Passenger Details</th>
                <th className="pb-3">Seats</th>
                <th className="pb-3">Boarding Terminal</th>
                <th className="pb-3">Fare Paid</th>
                <th className="pb-3">Status</th>
                <th className="pb-3 text-right pr-2">Operator Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/80">
              {filteredBookings.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-slate-500">
                    No matching passenger reservations found.
                  </td>
                </tr>
              ) : (
                filteredBookings.map((b) => (
                  <tr key={b.bookingReference} className="hover:bg-slate-950/40 transition-colors">
                    <td className="py-3.5 pl-2 font-mono font-bold text-white">
                      {b.bookingReference}
                    </td>
                    <td className="py-3.5">
                      <div className="font-semibold text-white">{b.passengerName || 'Nimal Silva'}</div>
                      <div className="text-[11px] text-slate-400 font-mono">{b.passengerPhone || '+94 77 123 4567'}</div>
                    </td>
                    <td className="py-3.5 font-mono">
                      <div className="flex gap-1.5">
                        {(Array.isArray(b.seatNumbers) ? b.seatNumbers : [b.seatNumbers]).map(s => (
                          <span key={s} className="px-2 py-0.5 rounded bg-slate-800 text-white font-bold text-[11px] border border-slate-700">
                            {s}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td className="py-3.5 text-slate-300">
                      {b.boardingPoint || 'Makumbura MMC (Platform 3)'}
                    </td>
                    <td className="py-3.5 font-mono font-semibold text-emerald-400">
                      {formatLkr(b.totalFareAmount || 5700.0)}
                    </td>
                    <td className="py-3.5">
                      <TransitBadge
                        status={
                          b.status === 'Confirmed'
                            ? 'Available'
                            : b.status === 'Boarded'
                            ? 'Luxury'
                            : 'Disrupted'
                        }
                        label={b.status}
                      />
                    </td>
                    <td className="py-3.5 text-right pr-2">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setInspectTicket(b)}
                          className="h-7 text-[11px] px-2.5"
                        >
                          <QrCode className="w-3 h-3 mr-1" />
                          E-Ticket
                        </Button>

                        {b.status === 'Confirmed' && (
                          <Button
                            variant="danger"
                            size="sm"
                            onClick={() => setCancelModalBooking(b)}
                            className="h-7 text-[11px] px-2.5 bg-rose-600/80 hover:bg-rose-600"
                          >
                            Cancel / Refund
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Bottom Section: Two Simulator Consoles (Sandbox Charge & Seat Hold) */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Simulator 1: Payment Sandbox Simulator */}
        <Card className="p-6">
          <div className="flex items-center justify-between pb-3 border-b border-slate-800 mb-4">
            <div>
              <h3 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                <CreditCard className="w-4 h-4 text-waypoint-amber" />
                Payment Sandbox Gateway Simulator
              </h3>
              <p className="text-xs text-slate-400 mt-0.5">
                Simulates card charges against the live ASP.NET Core `/payments/sandbox-charge` endpoint.
              </p>
            </div>
          </div>

          <form onSubmit={handleExecuteSandboxCharge} className="space-y-4">
            {/* Preset Card Chips */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                Preset Test Card Persona:
              </label>
              <div className="grid grid-cols-3 gap-2 text-xs">
                <button
                  type="button"
                  onClick={() => handlePresetChange('success')}
                  className={`p-2 rounded-lg border text-left transition-all ${
                    sandboxCardPreset === 'success'
                      ? 'bg-emerald-950/40 border-emerald-500/80 text-emerald-300 font-bold'
                      : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                  }`}
                >
                  <div className="font-semibold">Instant Success</div>
                  <div className="text-[10px] font-mono opacity-80">Ends in 0001 (200 OK)</div>
                </button>

                <button
                  type="button"
                  onClick={() => handlePresetChange('declined')}
                  className={`p-2 rounded-lg border text-left transition-all ${
                    sandboxCardPreset === 'declined'
                      ? 'bg-rose-950/40 border-rose-500/80 text-rose-300 font-bold'
                      : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                  }`}
                >
                  <div className="font-semibold">Card Declined</div>
                  <div className="text-[10px] font-mono opacity-80">Ends in 0002 (402)</div>
                </button>

                <button
                  type="button"
                  onClick={() => handlePresetChange('timeout')}
                  className={`p-2 rounded-lg border text-left transition-all ${
                    sandboxCardPreset === 'timeout'
                      ? 'bg-amber-950/40 border-amber-500/80 text-amber-300 font-bold'
                      : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                  }`}
                >
                  <div className="font-semibold">Gateway Timeout</div>
                  <div className="text-[10px] font-mono opacity-80">Ends in 0003 (504)</div>
                </button>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs text-slate-400 mb-1 font-mono">Card Number</label>
                <input
                  type="text"
                  value={sandboxCardNumber}
                  onChange={(e) => setSandboxCardNumber(e.target.value)}
                  className="w-full px-3 py-2 rounded-lg bg-slate-950 border border-slate-800 text-xs font-mono text-white focus:outline-none focus:border-waypoint-blue"
                />
              </div>

              <div>
                <label className="block text-xs text-slate-400 mb-1 font-mono">Amount (LKR)</label>
                <input
                  type="number"
                  value={sandboxAmount}
                  onChange={(e) => setSandboxAmount(e.target.value)}
                  className="w-full px-3 py-2 rounded-lg bg-slate-950 border border-slate-800 text-xs font-mono text-white focus:outline-none focus:border-waypoint-blue"
                />
              </div>
            </div>

            {/* Sandbox Result Banner */}
            {sandboxResult && (
              <div
                className={`p-3.5 rounded-xl border text-xs ${
                  sandboxResult.isSuccess
                    ? 'bg-emerald-950/30 border-emerald-800 text-emerald-300'
                    : 'bg-rose-950/30 border-rose-800 text-rose-300'
                }`}
              >
                <div className="font-bold flex items-center gap-1.5">
                  {sandboxResult.isSuccess ? <CheckCircle2 className="w-4 h-4" /> : <XCircle className="w-4 h-4" />}
                  Status: {sandboxResult.gatewayStatus}
                  {sandboxResult.transactionId && ` • ${sandboxResult.transactionId}`}
                </div>
                <p className="mt-1 opacity-90">{sandboxResult.message}</p>
              </div>
            )}

            <Button
              type="submit"
              variant="primary"
              size="sm"
              disabled={sandboxLoading}
              className="w-full text-xs"
            >
              <Play className="w-3.5 h-3.5 mr-1.5" />
              {sandboxLoading ? 'Processing Live Transaction...' : 'Execute Sandbox Card Charge'}
            </Button>
          </form>
        </Card>

        {/* Simulator 2: 10-Min Seat Hold & Concurrency Tester */}
        <Card className="p-6">
          <div className="flex items-center justify-between pb-3 border-b border-slate-800 mb-4">
            <div>
              <h3 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                <Clock className="w-4 h-4 text-waypoint-blue" />
                10-Minute Seat Hold & Concurrency Tester
              </h3>
              <p className="text-xs text-slate-400 mt-0.5">
                Tests server-side 409 Conflict checks against active PostgreSQL SeatHolds.
              </p>
            </div>
          </div>

          <form onSubmit={handleHoldSeats} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                Seat Numbers to Hold (comma-separated):
              </label>
              <input
                type="text"
                value={holdSeatsInput}
                onChange={(e) => setHoldSeatsInput(e.target.value)}
                placeholder="e.g. 4A, 4B or 12C"
                className="w-full px-3 py-2 rounded-lg bg-slate-950 border border-slate-800 text-xs font-mono text-white focus:outline-none focus:border-waypoint-blue"
              />
              <p className="text-[11px] text-slate-500 mt-1">
                Tip: Try holding <strong className="text-slate-300">4A, 4B</strong> (which are already held/booked) to verify the <strong>409 Conflict</strong> response.
              </p>
            </div>

            {/* Hold Result Banner */}
            {holdResult && (
              <div
                className={`p-3.5 rounded-xl border text-xs ${
                  holdResult.success
                    ? 'bg-emerald-950/30 border-emerald-800 text-emerald-300'
                    : 'bg-amber-950/30 border-amber-800 text-amber-300'
                }`}
              >
                <div className="font-bold flex items-center gap-1.5">
                  {holdResult.success ? <CheckCircle2 className="w-4 h-4" /> : <AlertTriangle className="w-4 h-4" />}
                  {holdResult.success ? 'Hold Created (200 OK)' : `Conflict Detected (HTTP ${holdResult.status || 409})`}
                </div>
                <p className="mt-1 opacity-90">{holdResult.message}</p>
              </div>
            )}

            <Button
              type="submit"
              variant="secondary"
              size="sm"
              disabled={holdLoading}
              className="w-full text-xs"
            >
              <RotateCcw className={`w-3.5 h-3.5 mr-1.5 ${holdLoading ? 'animate-spin' : ''}`} />
              {holdLoading ? 'Requesting Hold from PostgreSQL...' : 'Attempt 10-Minute Hold Request'}
            </Button>
          </form>
        </Card>
      </div>

      {/* Modal 1: Tiered Refund Modal (BR-REFUND-001) */}
      {cancelModalBooking && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
          <div className="w-full max-w-md p-6 rounded-2xl bg-slate-900 border border-slate-800 shadow-2xl">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800 mb-4">
              <div>
                <h3 className="text-base font-bold text-white font-display">
                  Operator Cancellation & Refund
                </h3>
                <p className="text-xs text-slate-400">
                  Ref: <span className="font-mono text-white font-bold">{cancelModalBooking.bookingReference}</span>
                </p>
              </div>
              <button
                onClick={() => setCancelModalBooking(null)}
                className="text-slate-400 hover:text-white p-1"
              >
                ✕
              </button>
            </div>

            {/* BR-REFUND-001 Policy Breakdown */}
            <div className="p-3.5 rounded-xl bg-slate-950 border border-slate-800 text-xs space-y-2 mb-4">
              <div className="flex justify-between">
                <span className="text-slate-400">Departure Offset:</span>
                <span className="font-semibold text-white">{cancelModalBooking.hoursUntilDeparture} hours until departure</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-400">Policy Tier:</span>
                <span className="font-semibold text-emerald-400">
                  {cancelModalBooking.hoursUntilDeparture > 24
                    ? 'Tier 1: 90% Refund (10% platform fee)'
                    : cancelModalBooking.hoursUntilDeparture >= 12
                    ? 'Tier 2: 50% Refund (50% late fee)'
                    : 'Tier 3: 0% Non-refundable'}
                </span>
              </div>
              <div className="border-t border-slate-800 pt-2 flex justify-between font-bold">
                <span className="text-slate-300">Net Refund Credited:</span>
                <span className="text-emerald-400 font-mono">
                  {formatLkr(
                    cancelModalBooking.totalFareAmount *
                      (cancelModalBooking.hoursUntilDeparture > 24
                        ? 0.90
                        : cancelModalBooking.hoursUntilDeparture >= 12
                        ? 0.50
                        : 0.0)
                  )}
                </span>
              </div>
            </div>

            <div className="mb-4">
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                Cancellation Reason:
              </label>
              <select
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                className="w-full px-3 py-2 rounded-lg bg-slate-950 border border-slate-800 text-xs text-white focus:outline-none focus:border-waypoint-blue"
              >
                <option value="Change of travel plans">Change of travel plans</option>
                <option value="Passenger medical emergency">Passenger medical emergency</option>
                <option value="Alternative route arranged">Alternative route arranged</option>
                <option value="Operator operational change">Operator operational change</option>
              </select>
            </div>

            <div className="flex justify-end gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setCancelModalBooking(null)}
              >
                Keep Booking
              </Button>
              <Button
                variant="danger"
                size="sm"
                disabled={cancelLoading}
                onClick={handleConfirmCancellation}
              >
                {cancelLoading ? 'Cancelling in Database...' : 'Confirm Cancellation & Refund'}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Modal 2: Inspect E-Ticket Pass */}
      {inspectTicket && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
          <div className="w-full max-w-sm p-6 rounded-2xl bg-slate-900 border border-slate-800 shadow-2xl text-center">
            <div className="flex justify-between items-center mb-4">
              <TransitBadge status="Luxury" label="Official Boarding Pass" />
              <button
                onClick={() => setInspectTicket(null)}
                className="text-slate-400 hover:text-white p-1"
              >
                ✕
              </button>
            </div>

            <div className="p-4 rounded-xl bg-white text-slate-900 font-mono text-xs mb-4">
              <div className="text-[10px] text-slate-500 uppercase">Sri Lanka Transit Board</div>
              <div className="text-base font-bold text-slate-900 mt-1">{inspectTicket.bookingReference}</div>
              <div className="text-xs text-slate-700 mt-0.5">{inspectTicket.passengerName}</div>
              <div className="text-xs font-bold text-blue-700 mt-1">Seats: {inspectTicket.seatNumbers?.join(', ')}</div>
              
              <div className="my-3 p-3 bg-slate-100 rounded-lg border border-dashed border-slate-300">
                <QrCode className="w-32 h-32 mx-auto text-slate-900" />
                <div className="text-[9px] text-slate-500 mt-1 font-mono break-all">
                  {inspectTicket.ticketQrPayload}
                </div>
              </div>

              <div className="text-[10px] text-slate-500">
                HMAC-SHA256 Cryptographically Signed
              </div>
            </div>

            <Button
              variant="outline"
              size="sm"
              onClick={() => setInspectTicket(null)}
              className="w-full"
            >
              Dismiss
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
