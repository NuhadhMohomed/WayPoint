import React from 'react';

export function Card({ children, className = '', ...props }) {
  return (
    <div
      className={`bg-white rounded-xl border border-slate-200/80 shadow-sm p-6 ${className}`}
      {...props}
    >
      {children}
    </div>
  );
}

export function CardHeader({ title, description, action, children, className = '' }) {
  if (children) {
    return (
      <div className={`flex flex-col space-y-1.5 pb-4 border-b border-slate-100 mb-5 ${className}`}>
        {children}
      </div>
    );
  }
  return (
    <div className={`flex items-start justify-between pb-4 border-b border-slate-100 mb-5 ${className}`}>
      <div>
        <h3 className="text-lg font-semibold text-slate-900 tracking-tight font-heading">{title}</h3>
        {description && <p className="text-sm text-slate-500 mt-1">{description}</p>}
      </div>
      {action && <div className="flex-shrink-0 ml-4">{action}</div>}
    </div>
  );
}

export function CardTitle({ children, className = '' }) {
  return (
    <h3 className={`text-lg font-semibold text-slate-900 dark:text-white tracking-tight font-heading ${className}`}>
      {children}
    </h3>
  );
}

export function CardDescription({ children, className = '' }) {
  return (
    <p className={`text-sm text-slate-500 dark:text-slate-400 mt-1 ${className}`}>
      {children}
    </p>
  );
}

export function CardContent({ children, className = '' }) {
  return (
    <div className={`${className}`}>
      {children}
    </div>
  );
}
