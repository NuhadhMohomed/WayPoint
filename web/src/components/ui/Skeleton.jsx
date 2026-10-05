import React from 'react'

/**
 * Skeleton component renders animated placeholder shapes during asynchronous data fetches.
 *
 * @param {Object} props
 * @param {string} [props.className] - Sizing and positioning utility classes.
 * @param {'line'|'circle'|'rect'|'card'} [props.variant='line'] - Skeleton shape.
 * @param {number} [props.count=1] - Number of lines to repeat when variant is 'line'.
 */
export function Skeleton({
  className = '',
  variant = 'line',
  count = 1,
  ...props
}) {
  const baseClasses = 'animate-pulse rounded bg-slate-800/70 border border-slate-700/30'

  if (variant === 'circle') {
    return (
      <div
        className={`rounded-full ${baseClasses} ${className}`}
        aria-hidden="true"
        {...props}
      />
    )
  }

  if (variant === 'card') {
    return (
      <div
        className={`rounded-xl p-5 ${baseClasses} flex flex-col gap-3 ${className}`}
        aria-hidden="true"
        {...props}
      >
        <div className="h-4 w-1/3 rounded bg-slate-700/50" />
        <div className="h-7 w-2/3 rounded bg-slate-700/40" />
        <div className="h-3 w-1/2 rounded bg-slate-700/30" />
      </div>
    )
  }

  if (count > 1 && variant === 'line') {
    return (
      <div className="flex flex-col gap-2.5 w-full" aria-hidden="true">
        {Array.from({ length: count }).map((_, index) => (
          <div
            key={index}
            className={`h-4 rounded ${baseClasses} ${
              index === count - 1 ? 'w-4/5' : 'w-full'
            } ${className}`}
          />
        ))}
      </div>
    )
  }

  return (
    <div
      className={`${variant === 'line' ? 'h-4 w-full' : 'h-24 w-full'} ${baseClasses} ${className}`}
      aria-hidden="true"
      {...props}
    />
  )
}
