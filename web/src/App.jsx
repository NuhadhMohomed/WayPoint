import React from 'react'
import { Routes, Route } from 'react-router-dom'

export function App() {
  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-slate-100 flex items-center justify-center p-6">
      <div className="max-w-md w-full bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-6 shadow-sm text-center space-y-4">
        <div className="w-12 h-12 rounded-xl bg-indigo-600 text-white flex items-center justify-center mx-auto font-bold text-xl">
          W
        </div>
        <h1 className="text-xl font-bold tracking-tight">WayPoint Transit Platform</h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Core UI Primitives and Design System Initialized.
        </p>
      </div>
    </div>
  )
}

export default App
