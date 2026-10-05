import React from 'react';

export function TransitBadge({ children, variant = 'standard', className = '' }) {
  const variants = {
    luxury: 'bg-blue-50 text-blue-700 border-blue-200/80',
    'semi-luxury': 'bg-indigo-50 text-indigo-700 border-indigo-200/80',
    standard: 'bg-slate-100 text-slate-700 border-slate-200/80',
    active: 'bg-emerald-50 text-emerald-700 border-emerald-200/80',
    held: 'bg-amber-50 text-amber-800 border-amber-200/80',
    booked: 'bg-slate-100 text-slate-600 border-slate-200/80',
    critical: 'bg-red-50 text-red-700 border-red-200/80',
    warning: 'bg-amber-50 text-amber-700 border-amber-200/80'
  };

  const selectedVariant = variants[variant] || variants.standard;

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border font-mono tracking-wide select-none ${selectedVariant} ${className}`}
    >
      {children}
    </span>
  );
}
