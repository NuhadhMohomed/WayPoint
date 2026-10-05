import React, { forwardRef } from 'react'
import { X } from 'lucide-react'

/**
 * Standardized WayPoint form input component.
 * Follows DESIGN.md tokens: Inter font, Spring Green focus ring, and accessible error states.
 */
export const Input = forwardRef(function Input(
  {
    label,
    error,
    helperText,
    className = '',
    disabled = false,
    id,
    type = 'text',
    value,
    onClear,
    icon: Icon,
    required = false,
    ...props
  },
  ref
) {
  const inputId = id || (label ? label.toLowerCase().replace(/[^a-z0-9]/g, '-') : undefined)
  const errorId = error && inputId ? `${inputId}-error` : undefined
  const helperId = helperText && inputId ? `${inputId}-helper` : undefined

  return (
    <div className="w-full space-y-1.5 font-sans">
      {label && (
        <label
          htmlFor={inputId}
          className="block text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300"
        >
          {label} {required && <span className="text-red-500">*</span>}
        </label>
      )}

      <div className="relative flex items-center">
        {Icon && (
          <div className="pointer-events-none absolute left-3 flex items-center text-slate-400">
            <Icon className="h-4 w-4" />
          </div>
        )}

        <input
          ref={ref}
          id={inputId}
          type={type}
          value={value}
          disabled={disabled}
          aria-invalid={!!error}
          aria-describedby={errorId || helperId}
          className={`w-full rounded-lg border bg-slate-50 dark:bg-waypoint-darkSubdued/80 px-3.5 py-2.5 text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 transition-colors
            focus:border-waypoint-primary focus:outline-none focus:ring-2 focus:ring-waypoint-primary/40
            disabled:cursor-not-allowed disabled:opacity-50
            ${Icon ? 'pl-9' : ''}
            ${onClear && value ? 'pr-9' : ''}
            ${
              error
                ? 'border-red-500 focus:border-red-500 focus:ring-red-500/30'
                : 'border-slate-300 dark:border-waypoint-darkBorder hover:border-slate-400 dark:hover:border-slate-600'
            } ${className}`}
          {...props}
        />

        {onClear && value && !disabled && (
          <button
            type="button"
            onClick={onClear}
            className="absolute right-2.5 rounded-full p-1 text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 hover:text-slate-600 dark:hover:text-slate-200 transition-colors"
            title="Clear search"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      {error ? (
        <p id={errorId} className="text-xs font-medium text-red-500 dark:text-red-400">
          {error}
        </p>
      ) : helperText ? (
        <p id={helperId} className="text-xs text-slate-500 dark:text-waypoint-darkMuted">
          {helperText}
        </p>
      ) : null}
    </div>
  )
})
