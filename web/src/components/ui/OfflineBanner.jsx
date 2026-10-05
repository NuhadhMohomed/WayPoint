import React from 'react';
import { WifiOff } from 'lucide-react';

export function OfflineBanner({ isOffline }) {
  if (!isOffline) return null;

  return (
    <div className="w-full bg-amber-500 text-slate-900 px-4 py-2 text-xs font-semibold flex items-center justify-center gap-2 shadow-sm animate-pulse z-50">
      <WifiOff className="w-4 h-4 flex-shrink-0" />
      <span>Offline Mode — Changes may not be saved until internet connectivity is restored.</span>
    </div>
  );
}
