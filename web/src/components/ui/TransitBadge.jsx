import React from 'react'

export function TransitBadge({ status = 'Available', label, className = '' }) {
  const text = label || status

  const styles = {
    Available: "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20",
    Held: "bg-amber-500/10 text-amber-400 border border-amber-500/20",
    Booked: "bg-slate-500/10 text-slate-400 border border-slate-500/20",
    Disrupted: "bg-rose-500/10 text-rose-400 border border-rose-500/20",
    Delayed: "bg-orange-500/10 text-orange-400 border border-orange-500/20",
    Luxury: "bg-indigo-500/10 text-indigo-300 border border-indigo-500/20",
    Express: "bg-sky-500/10 text-sky-400 border border-sky-500/20",
  }

  return (
    <span 
      className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold tracking-wide ${styles[status] || styles.Available} ${className}`}
    >
      <span className="w-1.5 h-1.5 rounded-full mr-1.5 bg-current opacity-80" />
      {text}
    </span>
  )
}
