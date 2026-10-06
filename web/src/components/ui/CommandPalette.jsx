import React, { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useThemeStore } from '../../store/themeStore'

export const COMMAND_ITEMS = [
  // Navigation
  { id: 'nav-overview', title: 'System Overview & Live Departures', category: 'Navigation', path: '/overview', keywords: 'home dashboard stats kpi departures' },
  { id: 'nav-routes', title: 'Route Catalogue & Intermediate Stops', category: 'Navigation', path: '/routes', keywords: 'corridors waypoints stations network' },
  { id: 'nav-services', title: 'Service Scheduler & Timetables', category: 'Navigation', path: '/services', keywords: 'trips departures dispatch runs' },
  { id: 'nav-corridors', title: 'Scenic Tourist Corridors', category: 'Navigation', path: '/tourist-corridors', keywords: 'ella kandy coastal tea safari' },
  { id: 'nav-fleet-matrix', title: 'Fleet Matrix Builder', category: 'Navigation', path: '/fleet-matrix', keywords: 'vehicles buses trains aircon seating' },
  { id: 'nav-seat-designer', title: 'Seat Layout Designer', category: 'Navigation', path: '/seat-designer', keywords: 'tiers configuration window aisle' },
  { id: 'nav-driver-roster', title: 'Crew & Driver Rostering', category: 'Navigation', path: '/driver-roster', keywords: 'hos rest breaks compliance shifts' },
  { id: 'nav-fleet-reviews', title: 'Passenger Sentiment & Reviews', category: 'Navigation', path: '/fleet-reviews', keywords: 'feedback ratings cleanliness punctuality' },
  { id: 'nav-bookings', title: 'Manifest & Ticketing Monitor', category: 'Navigation', path: '/bookings', keywords: 'passengers refunds qr checkin print' },
  { id: 'nav-disruptions', title: 'Disruption Intake & Incident Desk', category: 'Navigation', path: '/disruptions', keywords: 'delays breakdowns weather protests' },
  { id: 'nav-approvals', title: 'Manager Approval Workbench', category: 'Navigation', path: '/manager-approvals', keywords: 'reroutes refunds emergency audits' },
  { id: 'nav-broadcasts', title: 'Passenger Alerts & Notifications', category: 'Navigation', path: '/alerts-broadcast', keywords: 'sms push notices corridor warnings' },
  { id: 'nav-ai-observability', title: 'AI Observability & Trace Waterfall', category: 'Navigation', path: '/ai-observability', keywords: 'agents gemini tokens execution latency' },
  { id: 'nav-admin', title: 'User Governance & Admin Console', category: 'Navigation', path: '/admin-console', keywords: 'security logs users rbac roles' },
  // Quick Actions
  { id: 'act-sidebar', title: 'Toggle Sidebar Collapse', category: 'Actions', action: 'toggle-sidebar', keywords: 'sidebar expand collapse width' },
]

/**
 * CommandPalette provides keyboard-first instantaneous fuzzy navigation (Ctrl+K / Cmd+K).
 */
export function CommandPalette({ isOpen, onClose }) {
  const [query, setQuery] = useState('')
  const [selectedIndex, setSelectedIndex] = useState(0)
  const navigate = useNavigate()
  const { toggleSidebar } = useThemeStore()
  const inputRef = useRef(null)

  useEffect(() => {
    if (isOpen) {
      setQuery('')
      setSelectedIndex(0)
      setTimeout(() => inputRef.current?.focus(), 50)
    }
  }, [isOpen])

  const filteredItems = COMMAND_ITEMS.filter((item) => {
    if (!query.trim()) return true
    const q = query.toLowerCase()
    return (
      item.title.toLowerCase().includes(q) ||
      item.category.toLowerCase().includes(q) ||
      item.keywords.toLowerCase().includes(q)
    )
  })

  useEffect(() => {
    setSelectedIndex(0)
  }, [query])

  const handleSelect = (item) => {
    if (!item) return
    onClose()

    if (item.path) {
      navigate(item.path)
    } else if (item.action === 'toggle-sidebar') {
      toggleSidebar()
    }
  }

  const handleKeyDown = (e) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setSelectedIndex((prev) => (prev + 1) % Math.max(1, filteredItems.length))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setSelectedIndex((prev) => (prev - 1 + filteredItems.length) % Math.max(1, filteredItems.length))
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (filteredItems[selectedIndex]) {
        handleSelect(filteredItems[selectedIndex])
      }
    } else if (e.key === 'Escape') {
      e.preventDefault()
      onClose()
    }
  }

  if (!isOpen) return null

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center pt-20 bg-slate-900/40 backdrop-blur-xs p-4"
      onClick={onClose}
    >
      <div
        className="w-full max-w-xl overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl transition-all"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-label="Global Command Palette"
      >
        <div className="flex items-center border-b border-slate-100 px-4 py-3 bg-white">
          <svg
            className="h-5 w-5 text-slate-400 mr-3"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
          </svg>
          <input
            ref={inputRef}
            type="text"
            className="w-full bg-transparent text-sm text-slate-900 placeholder-slate-400 outline-none"
            placeholder="Type a command or transit corridor... (e.g. Ella, Matrix, Roster)"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
          />
          <kbd className="hidden sm:inline-block rounded border border-slate-200 bg-slate-100 px-2 py-0.5 text-[10px] font-mono text-slate-500">
            ESC
          </kbd>
        </div>

        <div className="max-h-96 overflow-y-auto p-2">
          {filteredItems.length === 0 ? (
            <div className="py-8 text-center text-sm text-slate-400">
              No matching commands or corridors found.
            </div>
          ) : (
            <div className="space-y-1">
              {filteredItems.map((item, index) => {
                const isSelected = index === selectedIndex
                return (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => handleSelect(item)}
                    onMouseEnter={() => setSelectedIndex(index)}
                    className={`flex w-full items-center justify-between rounded-lg px-3 py-2.5 text-left text-sm transition-colors ${
                      isSelected
                        ? 'bg-indigo-600 text-white font-medium'
                        : 'text-slate-700 hover:bg-slate-50'
                    }`}
                  >
                    <div className="flex items-center gap-2.5 truncate">
                      <span
                        className={`text-[10px] uppercase font-bold tracking-wider px-1.5 py-0.5 rounded ${
                          isSelected
                            ? 'bg-black/20 text-white'
                            : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        {item.category}
                      </span>
                      <span className="truncate">{item.title}</span>
                    </div>
                    {isSelected && (
                      <span className="text-xs opacity-75 flex items-center gap-1 font-mono">
                        Jump ↵
                      </span>
                    )}
                  </button>
                )
              })}
            </div>
          )}
        </div>

        <div className="flex items-center justify-between border-t border-slate-100 bg-slate-50 px-4 py-2 text-[11px] text-slate-500">
          <span>Navigate with <kbd className="rounded bg-slate-200 px-1 font-mono text-slate-700">↑</kbd> <kbd className="rounded bg-slate-200 px-1 font-mono text-slate-700">↓</kbd></span>
          <span>Select with <kbd className="rounded bg-slate-200 px-1 font-mono text-slate-700">Enter</kbd></span>
        </div>
      </div>
    </div>
  )
}
