import React from 'react'

export function Card({ children, className = '', ...props }) {
  return (
    <div 
      className={`bg-slate-900 border border-slate-800 rounded-2xl shadow-sm p-6 ${className}`}
      {...props}
    >
      {children}
    </div>
  )
}

export function CardHeader({ title, subtitle, action, className = '' }) {
  return (
    <div className={`flex items-start justify-between pb-4 border-b border-slate-800/80 mb-5 ${className}`}>
      <div>
        <h3 className="text-lg font-semibold font-display text-white">{title}</h3>
        {subtitle && <p className="text-sm text-slate-400 mt-1">{subtitle}</p>}
      </div>
      {action && <div>{action}</div>}
    </div>
  )
}
