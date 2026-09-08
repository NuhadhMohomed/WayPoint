import React from 'react'

export function Button({ 
  children, 
  variant = 'primary', 
  size = 'md', 
  className = '', 
  disabled = false,
  ...props 
}) {
  const baseClasses = "inline-flex items-center justify-center font-medium rounded-lg transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none"
  
  const variantClasses = {
    primary: "bg-waypoint-blue text-white hover:bg-waypoint-darkBlue focus:ring-waypoint-blue shadow-sm",
    secondary: "bg-waypoint-amber text-slate-900 hover:bg-amber-500 focus:ring-waypoint-amber font-semibold",
    outline: "border border-slate-700 text-slate-200 hover:bg-slate-800 focus:ring-slate-400",
    ghost: "text-slate-400 hover:text-white hover:bg-slate-800/60",
    danger: "bg-red-600 text-white hover:bg-red-700 focus:ring-red-500",
    success: "bg-waypoint-green text-white hover:bg-green-700 focus:ring-green-500",
  }

  const sizeClasses = {
    sm: "text-xs px-3 py-1.5 h-8",
    md: "text-sm px-4 py-2 h-10",
    lg: "text-base px-5 py-2.5 h-12",
  }

  return (
    <button
      className={`${baseClasses} ${variantClasses[variant] || variantClasses.primary} ${sizeClasses[size] || sizeClasses.md} ${className}`}
      disabled={disabled}
      {...props}
    >
      {children}
    </button>
  )
}
