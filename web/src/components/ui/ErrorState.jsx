import React, { useState } from 'react'
import { Button } from './Button'

/**
 * ErrorState displays clear, empathetic error communication with actionable recovery.
 *
 * @param {Object} props
 * @param {string} [props.title='Unable to load data'] - Headline.
 * @param {string} props.message - User-friendly explanation.
 * @param {string} [props.technicalDetails] - Raw stack trace or API status code for NOC diagnostics.
 * @param {Function} [props.onRetry] - Callback to trigger re-fetch.
 * @param {string} [props.retryLabel='Retry'] - Label for retry button.
 * @param {string} [props.className] - Container CSS classes.
 */
export function ErrorState({
  title = 'Unable to load data',
  message,
  technicalDetails,
  onRetry,
  retryLabel = 'Retry',
  className = '',
}) {
  const [showTechnical, setShowTechnical] = useState(false)

  return (
    <div
      className={`rounded-xl border border-red-500/30 bg-red-950/20 p-6 text-center sm:text-left ${className}`}
      role="alert"
      data-testid="error-state"
    >
      <div className="flex flex-col sm:flex-row items-center sm:items-start gap-4">
        <div className="flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-xl bg-red-500/10 text-red-400 border border-red-500/20">
          <svg
            className="h-6 w-6"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={1.5}
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z"
            />
          </svg>
        </div>

        <div className="flex-1">
          <h3 className="text-base font-semibold text-red-200">{title}</h3>
          <p className="mt-1 text-sm text-red-300/80 leading-relaxed">{message}</p>

          {technicalDetails && (
            <div className="mt-3">
              <button
                type="button"
                onClick={() => setShowTechnical(!showTechnical)}
                className="text-xs font-mono text-red-400 hover:text-red-300 underline"
              >
                {showTechnical ? 'Hide diagnostic details' : 'Show diagnostic details'}
              </button>
              {showTechnical && (
                <pre className="mt-2 p-2.5 rounded bg-slate-950/80 border border-slate-800 text-[11px] font-mono text-slate-400 overflow-x-auto text-left whitespace-pre-wrap">
                  {technicalDetails}
                </pre>
              )}
            </div>
          )}

          {onRetry && (
            <div className="mt-4 flex justify-center sm:justify-start">
              <Button
                variant="danger"
                size="sm"
                onClick={onRetry}
                className="gap-2"
              >
                <svg
                  className="h-4 w-4"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth={2}
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99"
                  />
                </svg>
                {retryLabel}
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
