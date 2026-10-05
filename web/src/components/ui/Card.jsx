import React from 'react'

/**
 * WayPoint Double-Bezel Card Component
 * Conforms to modern transit cockpit standards with theme-aware borders and elevation.
 */
export function Card({ children, className = '', hover = false, ...props }) {
  return (
    <div 
      className={`bg-white dark:bg-waypoint-darkSurface border border-slate-200 dark:border-waypoint-darkBorder rounded-xl shadow-sm transition-all duration-200 ${
        hover ? 'hover:border-slate-300 dark:hover:border-slate-700 hover:shadow-md' : ''
      } ${className}`}
      {...props}
    >
      {children}
    </div>
  )
}

export function CardHeader({ title, subtitle, action, className = '', children }) {
  if (children) {
    return (
      <div className={`p-6 pb-4 border-b border-slate-100 dark:border-waypoint-darkBorder/60 flex items-start justify-between gap-4 ${className}`}>
        {children}
      </div>
    )
  }

  return (
    <div className={`p-6 pb-4 border-b border-slate-100 dark:border-waypoint-darkBorder/60 flex items-start justify-between gap-4 ${className}`}>
      <div>
        {title && <h3 className="text-lg font-bold font-display tracking-tight text-slate-900 dark:text-white">{title}</h3>}
        {subtitle && <p className="text-xs text-slate-500 dark:text-waypoint-darkMuted mt-1">{subtitle}</p>}
      </div>
      {action && <div className="flex-shrink-0">{action}</div>}
    </div>
  )
}

export function CardTitle({ children, className = '' }) {
  return (
    <h3 className={`text-base font-bold font-display tracking-tight text-slate-900 dark:text-white ${className}`}>
      {children}
    </h3>
  )
}

export function CardDescription({ children, className = '' }) {
  return (
    <p className={`text-xs text-slate-500 dark:text-waypoint-darkMuted mt-1 ${className}`}>
      {children}
    </p>
  )
}

export function CardContent({ children, className = '' }) {
  return (
    <div className={`p-6 ${className}`}>
      {children}
    </div>
  )
}

export function CardFooter({ children, className = '' }) {
  return (
    <div className={`p-6 pt-3 border-t border-slate-100 dark:border-waypoint-darkBorder/60 flex items-center justify-between gap-3 ${className}`}>
      {children}
    </div>
  )
}
