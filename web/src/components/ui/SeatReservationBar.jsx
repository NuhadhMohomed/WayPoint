import React, { useState, useEffect } from 'react'

/**
 * SeatReservationBar displays the 10-minute temporary seat hold countdown (FR-BOOKING-001).
 * Used during seat selection, manifest checking, and checkout flows.
 *
 * @param {Object} props
 * @param {number} [props.durationSeconds=600] - Total hold duration in seconds (defaults to 10 min).
 * @param {Function} [props.onExpired] - Callback invoked when the countdown reaches zero.
 * @param {string} [props.className] - Optional custom wrapper styling.
 */
export function SeatReservationBar({
  durationSeconds = 600,
  onExpired,
  className = '',
}) {
  const [secondsRemaining, setSecondsRemaining] = useState(durationSeconds)

  useEffect(() => {
    setSecondsRemaining(durationSeconds)
  }, [durationSeconds])

  useEffect(() => {
    if (secondsRemaining <= 0) {
      if (onExpired) onExpired()
      return
    }

    const timer = setInterval(() => {
      setSecondsRemaining((prev) => {
        if (prev <= 1) {
          clearInterval(timer)
          if (onExpired) onExpired()
          return 0
        }
        return prev - 1
      })
    }, 1000)

    return () => clearInterval(timer)
  }, [secondsRemaining, onExpired])

  const minutes = Math.floor(secondsRemaining / 60)
  const seconds = secondsRemaining % 60
  const formattedTime = `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`

  const progressPercent = Math.max(0, Math.min(100, (secondsRemaining / durationSeconds) * 100))
  const isExpiringSoon = secondsRemaining <= 120 // Last 2 minutes

  return (
    <div
      className={`rounded-lg border px-4 py-3 shadow-sm transition-colors ${
        isExpiringSoon
          ? 'border-amber-500/50 bg-amber-950/20 text-amber-200'
          : 'border-slate-800 bg-slate-900/60 text-slate-200'
      } ${className}`}
    >
      <div className="flex items-center justify-between text-xs font-medium">
        <div className="flex items-center gap-2">
          <span
            className={`inline-block h-2 w-2 rounded-full ${
              secondsRemaining === 0
                ? 'bg-red-500'
                : isExpiringSoon
                ? 'animate-pulse bg-waypoint-amber'
                : 'bg-waypoint-blue'
            }`}
          />
          <span>
            {secondsRemaining === 0
              ? 'Seat hold expired. Please reselect your seats.'
              : isExpiringSoon
              ? 'Seat hold expiring soon! Complete booking to secure your seats.'
              : 'Seats temporarily reserved'}
          </span>
        </div>
        <span className="font-mono text-sm font-semibold tracking-wide">
          {formattedTime}
        </span>
      </div>

      <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-slate-800">
        <div
          className={`h-full transition-all duration-1000 ease-linear ${
            isExpiringSoon ? 'bg-waypoint-amber' : 'bg-waypoint-blue'
          }`}
          style={{ width: `${progressPercent}%` }}
        />
      </div>
    </div>
  )
}
