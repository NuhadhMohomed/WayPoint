import React from 'react'

/**
 * WayPoint Standard Button Component
 * Synchronized with mobile tokens: Spring Green (#32DE84) & Forest Black (#042611) text (WCAG AAA >= 7:1)
 *
 * @param {Object} props
 * @param {'primary'|'secondary'|'tertiary'|'outline'|'ghost'|'danger'} [props.variant='primary']
 * @param {'sm'|'md'|'lg'} [props.size='md']
 * @param {boolean} [props.isLoading=false]
 * @param {boolean} [props.disabled=false]
 */
export function Button({ 
  children, 
  variant = 'primary', 
  size = 'md', 
  className = '', 
  disabled = false,
  isLoading = false,
  type = 'button',
  icon: Icon,
  ...props 
}) {
  const baseClasses = "inline-flex items-center justify-center font-medium rounded-lg transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 active:scale-[0.98] disabled:opacity-50 disabled:pointer-events-none select-none"
  
  const variantClasses = {
    primary: "bg-waypoint-primary text-waypoint-onPrimary hover:bg-waypoint-primaryDark font-semibold shadow-sm focus:ring-waypoint-primary focus:ring-offset-background",
    secondary: "bg-waypoint-amber text-slate-950 hover:bg-amber-500 font-semibold shadow-sm focus:ring-waypoint-amber focus:ring-offset-background",
    tertiary: "bg-waypoint-sky text-white hover:bg-sky-600 font-medium shadow-sm focus:ring-waypoint-sky focus:ring-offset-background",
    outline: "border border-slate-300 dark:border-waypoint-darkBorder text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-waypoint-darkSubdued focus:ring-slate-400 focus:ring-offset-background",
    ghost: "text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-waypoint-darkSubdued/60 focus:ring-slate-400 focus:ring-offset-background",
    danger: "bg-waypoint-error text-white hover:bg-red-600 font-medium shadow-sm focus:ring-red-500 focus:ring-offset-background",
  }

  const sizeClasses = {
    sm: "text-xs px-3 py-1.5 h-8 gap-1.5",
    md: "text-sm px-4 py-2.5 h-10 gap-2",
    lg: "text-base px-5 py-3 h-12 gap-2.5 font-semibold",
  }

  return (
    <button
      type={type}
      className={`${baseClasses} ${variantClasses[variant] || variantClasses.primary} ${sizeClasses[size] || sizeClasses.md} ${className}`}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? (
        <>
          <svg className="animate-spin -ml-0.5 mr-2 h-4 w-4 text-current" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span>{children}</span>
        </>
      ) : (
        <>
          {Icon && <Icon className="w-4 h-4 flex-shrink-0" />}
          <span>{children}</span>
        </>
      )}
    </button>
  )
}
