import React from 'react'
import { Modal } from './Modal'

const SHORTCUT_GROUPS = [
  {
    group: 'Global Navigation & Command',
    shortcuts: [
      { key: 'Ctrl + K / ⌘K', description: 'Open Global Command Palette & Hub Jump' },
      { key: '?', description: 'Display this Keyboard Shortcuts Reference Sheet' },
      { key: 'Esc', description: 'Dismiss open modals, command palettes, and side drawers' },
    ],
  },
  {
    group: 'Operational Controls & Viewport',
    shortcuts: [
      { key: 'Alt + T', description: 'Toggle Dark / Light Mode Palette' },
    ],
  },
  {
    group: 'Manifest & Data Operations',
    shortcuts: [
      { key: 'Ctrl + P / ⌘P', description: 'Open A4 High-Contrast Departure Manifest Print Preview' },
      { key: 'Ctrl + E', description: 'Trigger Rapid CSV Data Export' },
    ],
  },
]

/**
 * KeyboardShortcutsModal presents the keyboard shortcut reference sheet.
 */
export function KeyboardShortcutsModal({ isOpen, onClose }) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="NOC Keyboard Shortcuts Cheat-Sheet"
      maxWidth="max-w-xl"
    >
      <div className="space-y-6">
        <p className="text-xs text-slate-400">
          Supercharge your dispatch and operations workflow with instant keyboard accelerators.
        </p>

        {SHORTCUT_GROUPS.map((group, idx) => (
          <div key={idx} className="space-y-2">
            <h4 className="text-xs font-bold uppercase tracking-wider text-indigo-400">
              {group.group}
            </h4>
            <div className="divide-y divide-slate-800 rounded-xl border border-slate-800 bg-slate-950/50">
              {group.shortcuts.map((sc, scIdx) => (
                <div
                  key={scIdx}
                  className="flex items-center justify-between px-3.5 py-2.5 text-xs"
                >
                  <span className="text-slate-300">{sc.description}</span>
                  <kbd className="rounded-md border border-slate-700 bg-slate-800/80 px-2 py-1 font-mono text-[11px] font-semibold text-slate-200 shadow-sm">
                    {sc.key}
                  </kbd>
                </div>
              ))}
            </div>
          </div>
        ))}

        <div className="flex justify-end pt-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg bg-slate-800 px-4 py-2 text-xs font-semibold text-slate-200 hover:bg-slate-700 transition-colors"
          >
            Got it (Esc)
          </button>
        </div>
      </div>
    </Modal>
  )
}
