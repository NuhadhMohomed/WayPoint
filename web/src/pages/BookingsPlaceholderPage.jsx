import React from 'react'
import { Ticket, GitBranch, Database, FileText, CreditCard } from 'lucide-react'

export function BookingsPlaceholderPage() {
  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-sky-500/20 text-sky-300 border border-sky-500/30">
              Component 3
            </span>
            <span className="text-xs text-slate-400 font-medium">Assigned to <strong className="text-white">Mithila</strong></span>
          </div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Booking, Ticketing & Passenger Options</h1>
          <p className="text-xs text-slate-400 mt-1">10-min temporary seat holds, payment sandbox checkout, QR e-tickets, and cancellations.</p>
        </div>
        <div className="flex items-center gap-1.5 text-xs font-mono text-slate-400 bg-slate-950 px-3 py-1.5 rounded-lg border border-slate-800">
          <GitBranch className="w-4 h-4 text-sky-400" />
          feature/c3-booking-ticketing
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-sky-400 font-semibold text-xs mb-2">
            <Database className="w-4 h-4" />
            PostgreSQL Domain Entities
          </div>
          <ul className="text-xs text-slate-400 space-y-1 font-mono">
            <li>• SeatHolds (10-min lock)</li>
            <li>• Bookings</li>
            <li>• Tickets (QR Payload)</li>
            <li>• PaymentAttempts</li>
            <li>• Refunds</li>
          </ul>
        </div>

        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-indigo-400 font-semibold text-xs mb-2">
            <FileText className="w-4 h-4" />
            Target API Endpoints
          </div>
          <ul className="text-xs text-slate-400 space-y-1 font-mono">
            <li>• POST /api/v1/bookings/hold</li>
            <li>• POST /api/v1/bookings/confirm</li>
            <li>• GET /api/v1/bookings/my</li>
            <li>• POST /api/v1/bookings/cancel</li>
          </ul>
        </div>

        <div className="p-5 rounded-xl bg-slate-950 border border-slate-800">
          <div className="flex items-center gap-2 text-emerald-400 font-semibold text-xs mb-2">
            <CreditCard className="w-4 h-4" />
            Complex Business Operation
          </div>
          <p className="text-xs text-slate-400 leading-relaxed">
            Transactional Seat Hold & Payment Confirmation with PostgreSQL concurrency protection against double-booking.
          </p>
        </div>
      </div>

      <div className="p-8 rounded-xl bg-slate-950 border border-dashed border-slate-800 text-center">
        <Ticket className="w-8 h-8 text-sky-400 mx-auto mb-2 opacity-80" />
        <h3 className="text-base font-semibold text-white">Booking & Ticketing Workspace Ready</h3>
        <p className="text-xs text-slate-400 max-w-md mx-auto mt-1">
          Mithila can now switch to branch <code className="text-sky-300 font-mono">feature/c3-booking-ticketing</code> to build booking lists, payment confirmation workflows, and QR ticket verification.
        </p>
      </div>
    </div>
  )
}
