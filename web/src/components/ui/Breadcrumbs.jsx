import React from 'react'
import { Link } from 'react-router-dom'

/**
 * Breadcrumbs provides accessible navigational path hierarchy for nested NOC views.
 *
 * @param {Object} props
 * @param {Array<{label: string, href?: string}>} props.items - Ordered list of crumb items.
 * @param {string} [props.className] - Additional classes.
 */
export function Breadcrumbs({ items = [], className = '' }) {
  if (!items.length) return null

  return (
    <nav aria-label="Breadcrumb" className={`flex items-center text-xs ${className}`}>
      <ol className="flex items-center gap-1.5 flex-wrap">
        <li>
          <Link
            to="/overview"
            className="text-slate-400 hover:text-waypoint-primary transition-colors flex items-center gap-1"
          >
            <svg
              className="h-3.5 w-3.5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={2}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25"
              />
            </svg>
            <span>NOC</span>
          </Link>
        </li>

        {items.map((item, index) => {
          const isLast = index === items.length - 1

          return (
            <React.Fragment key={index}>
              <li className="text-slate-600 select-none" aria-hidden="true">
                /
              </li>
              <li>
                {isLast || !item.href ? (
                  <span
                    className="font-medium text-slate-200"
                    aria-current={isLast ? 'page' : undefined}
                  >
                    {item.label}
                  </span>
                ) : (
                  <Link
                    to={item.href}
                    className="text-slate-400 hover:text-waypoint-primary transition-colors"
                  >
                    {item.label}
                  </Link>
                )}
              </li>
            </React.Fragment>
          )
        })}
      </ol>
    </nav>
  )
}
