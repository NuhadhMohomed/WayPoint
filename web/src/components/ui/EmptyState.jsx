import React from 'react'
import { Button } from './Button'

/**
 * EmptyState displays a helpful illustration or icon with explanatory text and action CTA.
 *
 * @param {Object} props
 * @param {React.ReactNode} [props.icon] - Optional custom SVG icon.
 * @param {string} props.title - Main headline.
 * @param {string} [props.description] - Sub-text explaining next steps.
 * @param {string} [props.actionLabel] - Label for action button.
 * @param {Function} [props.onAction] - Click callback for action button.
 * @param {React.ReactNode} [props.children] - Additional custom content.
 * @param {string} [props.className] - Container CSS classes.
 */
export function EmptyState({
  icon,
  title,
  description,
  actionLabel,
  onAction,
  children,
  className = '',
}) {
  return (
    <div
      className={`flex flex-col items-center justify-center p-8 text-center rounded-xl border border-dashed border-slate-800 bg-slate-900/30 ${className}`}
      data-testid="empty-state"
    >
      <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-800/60 text-slate-400 border border-slate-700/50 shadow-inner">
        {icon || (
          <svg
            className="h-7 w-7 text-slate-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={1.5}
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5m6 4.125l2.25 2.25m0 0l2.25 2.25M12 13.875l2.25-2.25M12 13.875l-2.25 2.25M3.75 7.5h16.5m-16.5 0a2.25 2.25 0 012.25-2.25h12a2.25 2.25 0 012.25 2.25"
            />
          </svg>
        )}
      </div>

      <h3 className="text-base font-semibold text-slate-200">{title}</h3>
      {description && (
        <p className="mt-1.5 max-w-sm text-sm text-slate-400 leading-relaxed">
          {description}
        </p>
      )}

      {actionLabel && onAction && (
        <div className="mt-5">
          <Button variant="outline" size="sm" onClick={onAction}>
            {actionLabel}
          </Button>
        </div>
      )}

      {children && <div className="mt-4">{children}</div>}
    </div>
  )
}
