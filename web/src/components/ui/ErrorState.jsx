import React from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { Button } from './Button';

export function ErrorState({
  title = 'Failed to load data',
  message = 'An unexpected error occurred while communicating with the server.',
  onRetry,
  className = ''
}) {
  return (
    <div className={`flex flex-col items-center justify-center p-8 text-center bg-red-50/50 rounded-xl border border-red-200/80 ${className}`}>
      <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center mb-4 text-red-600">
        <AlertTriangle className="w-6 h-6" />
      </div>
      <h3 className="text-base font-semibold text-red-900 font-heading">{title}</h3>
      <p className="text-sm text-red-600 max-w-md mt-1 mb-5">{message}</p>
      {onRetry && (
        <Button onClick={onRetry} variant="outline" size="sm" className="border-red-200 text-red-700 hover:bg-red-50">
          <RefreshCw className="w-3.5 h-3.5 mr-2" />
          Retry Request
        </Button>
      )}
    </div>
  );
}
