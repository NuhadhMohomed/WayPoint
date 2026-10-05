import React, { useState, useEffect } from 'react'

/**
 * OfflineBanner monitors navigator connectivity and displays a persistent alert
 * when connectivity to the backend or internet is severed.
 */
export function OfflineBanner() {
  const [isOnline, setIsOnline] = useState(
    typeof navigator !== 'undefined' ? navigator.onLine : true
  )

  useEffect(() => {
    function handleOnline() {
      setIsOnline(true)
    }
    function handleOffline() {
      setIsOnline(false)
    }

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  if (isOnline) return null

  return (
    <div
      role="status"
      aria-live="polite"
      className="sticky top-0 z-50 flex items-center justify-between border-b border-amber-500/40 bg-amber-950/90 px-4 py-2 text-xs font-medium text-amber-200 backdrop-blur-md"
    >
      <div className="flex items-center gap-2">
        <span className="relative flex h-2 w-2">
          <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-amber-400 opacity-75" />
          <span className="relative inline-flex h-2 w-2 rounded-full bg-amber-500" />
        </span>
        <span>
          <strong>Offline Mode:</strong> Network disconnected. Displaying cached operational data. Real-time fleet telematics paused.
        </span>
      </div>
      <button
        onClick={() => window.location.reload()}
        className="rounded border border-amber-500/50 bg-amber-900/50 px-2 py-0.5 text-[11px] font-semibold hover:bg-amber-800/60 transition-colors"
      >
        Check Connection
      </button>
    </div>
  )
}
