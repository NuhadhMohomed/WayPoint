import React from 'react';
import { Loader2 } from 'lucide-react';

export function Button({
  children,
  type = 'button',
  variant = 'primary',
  size = 'md',
  isLoading = false,
  disabled = false,
  className = '',
  onClick,
  ...props
}) {
  const baseStyles = 'inline-flex items-center justify-center font-medium rounded-lg transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed select-none';

  const variants = {
    primary: 'bg-blue-600 hover:bg-blue-700 text-white shadow-sm focus:ring-blue-500 active:bg-blue-800',
    secondary: 'bg-slate-100 hover:bg-slate-200 text-slate-800 focus:ring-slate-400 active:bg-slate-300',
    outline: 'border border-slate-300 hover:bg-slate-50 text-slate-700 focus:ring-blue-500 bg-white shadow-sm',
    destructive: 'bg-red-600 hover:bg-red-700 text-white shadow-sm focus:ring-red-500 active:bg-red-800',
    amber: 'bg-amber-500 hover:bg-amber-600 text-slate-900 font-semibold shadow-sm focus:ring-amber-400 active:bg-amber-700',
    ghost: 'hover:bg-slate-100 text-slate-700 focus:ring-slate-400'
  };

  const sizes = {
    sm: 'text-xs px-2.5 py-1.5 gap-1.5',
    md: 'text-sm px-4 py-2 gap-2',
    lg: 'text-base px-5 py-2.5 gap-2.5'
  };

  const variantClass = variants[variant] || variants.primary;
  const sizeClass = sizes[size] || sizes.md;

  return (
    <button
      type={type}
      disabled={disabled || isLoading}
      onClick={onClick}
      className={`${baseStyles} ${variantClass} ${sizeClass} ${className}`}
      {...props}
    >
      {isLoading && (
        <Loader2
          data-testid="loading-spinner"
          className="w-4 h-4 animate-spin text-current"
        />
      )}
      {children}
    </button>
  );
}
