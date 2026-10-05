import React from 'react'

/**
 * WayPoint Transit Status Chip / Badge
 * Conforms to authoritative status colors (Spring Green, Sunset Amber, Sky Blue, Crimson Alert)
 */
export function TransitBadge({ status = 'Available', label, className = '' }) {
  const text = label || status

  const styles = {
    Available: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    Held: "bg-amber-500/10 text-amber-400 border-amber-500/20",
    Booked: "bg-slate-500/10 text-slate-400 border-slate-500/20",
    Blocked: "bg-slate-800 text-slate-500 border-slate-700",
    Disrupted: "bg-red-500/10 text-red-400 border-red-500/20",
    Delayed: "bg-orange-500/10 text-orange-400 border-orange-500/20",
    Cancelled: "bg-red-500/10 text-red-400 border-red-500/20",
    Confirmed: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    Pending: "bg-amber-500/10 text-amber-400 border-amber-500/20",
    Luxury: "bg-waypoint-primary/10 text-waypoint-primary border-waypoint-primary/30",
    Standard: "bg-slate-500/10 text-slate-400 border-slate-500/20",
    Express: "bg-waypoint-sky/10 text-waypoint-sky border-waypoint-sky/30",
    Active: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    Inactive: "bg-slate-500/10 text-slate-400 border-slate-500/20",
    Maintenance: "bg-orange-500/10 text-orange-400 border-orange-500/20",
  }

  const activeStyle = styles[status] || styles.Available

  return (
    <span 
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold tracking-wide border ${activeStyle} ${className}`}
    >
      <span className="w-1.5 h-1.5 rounded-full mr-1.5 bg-current opacity-80" />
      {text}
    </span>
  )
}
