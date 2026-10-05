import React from 'react'
import { Link } from 'react-router-dom'
import { Button } from '../components/ui/Button'

/**
 * NotFoundPage (404) renders when an unknown transit corridor, route, or resource is requested.
 */
export function NotFoundPage() {
  return (
    <div className="min-h-screen bg-slate-950 flex flex-col items-center justify-center p-6 text-center select-none text-slate-100">
      <div className="relative mb-8">
        <div className="text-9xl font-black tracking-widest text-slate-800/80 font-mono">
          404
        </div>
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="flex h-20 w-20 items-center justify-center rounded-2xl bg-slate-900 border border-slate-700/80 shadow-2xl text-indigo-400">
            <svg
              className="h-10 w-10 animate-pulse"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={1.75}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M9 6.75V15m6-6v8.25m.503 3.498l4.875-2.437c.381-.19.622-.58.622-1.006V4.82c0-.836-.88-1.38-1.628-1.006l-3.869 1.934c-.317.159-.69.159-1.006 0L9.503 3.252a1.125 1.125 0 00-1.006 0L3.622 5.689A1.125 1.125 0 003 6.695V19.18c0 .836.88 1.38 1.628 1.006l3.869-1.934c.317-.159.69-.159 1.006 0l4.994 2.497c.317.158.69.158 1.006 0z"
              />
            </svg>
          </div>
        </div>
      </div>

      <div className="inline-block px-3 py-1 rounded-full bg-amber-500/10 border border-amber-500/30 text-amber-300 text-xs font-semibold mb-4">
        Unscheduled Destination • Off Network
      </div>

      <h1 className="text-2xl sm:text-3xl font-bold tracking-tight max-w-md">
        Corridor Not Found
      </h1>
      <p className="mt-2 text-sm text-slate-400 max-w-md leading-relaxed">
        The requested transit view or waypoint does not exist in the National Operations Directory. It may have been rerouted or decommissioned.
      </p>

      <div className="mt-8 flex flex-wrap items-center justify-center gap-4">
        <Link to="/">
          <Button variant="primary" size="md">
            Return to Operations Cockpit
          </Button>
        </Link>
        <Link to="/routes">
          <Button variant="outline" size="md">
            Browse Active Catalog
          </Button>
        </Link>
      </div>

      <div className="mt-12 text-xs font-mono text-slate-600">
        WayPoint NOC Telematics Engine • Error Code: ERR_WAYPOINT_404
      </div>
    </div>
  )
}
export default NotFoundPage
