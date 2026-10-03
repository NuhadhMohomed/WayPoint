import React, { forwardRef } from 'react'

/**
 * Standardized WayPoint form input component.
 * Follows DESIGN.md tokens: Inter font, Lanka Blue focus ring, and clear error states.
 *
 * @param {Object} props
 * @param {string} [props.label] - Field label displayed above the input.
 * @param {string} [props.error] - Validation error message displayed below the input.
 * @param {string} [props.helperText] - Supporting helper text.
 * @param {string} [props.className] - Additional CSS classes.
 * @param {boolean} [props.disabled] - Disabled state.
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
    ...props
  },
  ref
) {
  const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined)

  return (
    <div className="w-full space-y-1.5 font-sans">
      {label && (
        <label
          htmlFor={inputId}
          className="block text-xs font-semibold uppercase tracking-wider text-slate-300"
        >
          {label}
        </label>
      )}

      <input
        ref={ref}
        id={inputId}
        type={type}
        disabled={disabled}
        className={`w-full rounded-md border bg-slate-900/80 px-3.5 py-2.5 text-sm text-slate-100 placeholder-slate-500 transition-colors
          focus:border-waypoint-blue focus:outline-none focus:ring-2 focus:ring-waypoint-blue/50
          disabled:cursor-not-allowed disabled:opacity-50
          ${
            error
              ? 'border-red-500 focus:border-red-500 focus:ring-red-500/50'
              : 'border-slate-700 hover:border-slate-600'
          } ${className}`}
        {...props}
      />

      {error ? (
        <p className="text-xs font-medium text-red-400">{error}</p>
      ) : helperText ? (
        <p className="text-xs text-slate-400">{helperText}</p>
      ) : null}
    </div>
  )
})
