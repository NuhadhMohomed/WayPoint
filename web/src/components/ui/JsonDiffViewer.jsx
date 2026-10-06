import React, { useMemo } from 'react'

/**
 * JsonDiffViewer renders formatted comparison between two JSON structures
 * for audit trail verification (FR-AUDIT-001).
 *
 * @param {Object} props
 * @param {Object|string} [props.oldData] - Baseline state
 * @param {Object|string} [props.newData] - Mutated state
 * @param {string} [props.title] - Optional label
 * @param {string} [props.className] - Container CSS
 */
export function JsonDiffViewer({
  oldData = {},
  newData = {},
  title = 'Audit State Mutation Diff',
  className = '',
}) {
  const diffLines = useMemo(() => {
    const oldStr = typeof oldData === 'string' ? oldData : JSON.stringify(oldData, null, 2)
    const newStr = typeof newData === 'string' ? newData : JSON.stringify(newData, null, 2)

    const oldLines = oldStr.split('\n')
    const newLines = newStr.split('\n')
    const result = []

    const maxLines = Math.max(oldLines.length, newLines.length)
    for (let i = 0; i < maxLines; i++) {
      const o = oldLines[i]
      const n = newLines[i]

      if (o === n) {
        result.push({ type: 'unchanged', text: o ?? n, lineNum: i + 1 })
      } else {
        if (o !== undefined) {
          result.push({ type: 'removed', text: o, lineNum: i + 1 })
        }
        if (n !== undefined) {
          result.push({ type: 'added', text: n, lineNum: i + 1 })
        }
      }
    }
    return result
  }, [oldData, newData])

  return (
    <div
      className={`rounded-xl border border-slate-200 bg-white font-mono text-xs overflow-hidden shadow-xs ${className}`}
      data-testid="json-diff-viewer"
    >
      <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50 px-4 py-2 text-slate-700">
        <span className="font-semibold">{title}</span>
        <div className="flex items-center gap-3 text-[11px]">
          <span className="flex items-center gap-1 text-emerald-700 font-medium">
            <span className="h-2 w-2 rounded-full bg-emerald-500 inline-block" /> Added
          </span>
          <span className="flex items-center gap-1 text-red-700 font-medium">
            <span className="h-2 w-2 rounded-full bg-red-500 inline-block" /> Removed
          </span>
        </div>
      </div>

      <div className="max-h-80 overflow-y-auto p-3 space-y-0.5 leading-5 font-mono select-text">
        {diffLines.map((line, idx) => {
          let bgClass = 'text-slate-600 hover:bg-slate-50'
          let sign = ' '
          let signColor = 'text-slate-400'

          if (line.type === 'added') {
            bgClass = 'bg-emerald-50 text-emerald-800 border-l-2 border-emerald-600 pl-1'
            sign = '+'
            signColor = 'text-emerald-700 font-bold'
          } else if (line.type === 'removed') {
            bgClass = 'bg-red-50 text-red-800 border-l-2 border-red-600 pl-1'
            sign = '-'
            signColor = 'text-red-700 font-bold'
          }

          return (
            <div key={idx} className={`flex items-start gap-2 py-0.5 px-2 rounded-sm ${bgClass}`}>
              <span className={`w-3 select-none text-right font-bold ${signColor}`}>{sign}</span>
              <span className="flex-1 whitespace-pre-wrap break-all">{line.text}</span>
            </div>
          )
        })}
      </div>
    </div>
  )
}
