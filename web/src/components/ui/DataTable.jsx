import React, { useState, useMemo } from 'react'
import { ArrowUpDown, ChevronLeft, ChevronRight, Download } from 'lucide-react'
import { Button } from './Button'
import { downloadCsv } from '../../lib/csvExport'

/**
 * Reusable Enterprise Transit DataTable
 * Supports column sorting, pagination, loading skeletons, CSV export, and empty fallback.
 */
export function DataTable({
  columns = [],
  data = [],
  isLoading = false,
  pageSize = 10,
  searchQuery = '',
  emptyTitle = 'No data available',
  emptyDescription = 'There are no records to display.',
  exportFilename = 'export.csv',
  showExport = true,
  className = '',
}) {
  const [sortField, setSortField] = useState(null)
  const [sortDirection, setSortDirection] = useState('asc')
  const [currentPage, setCurrentPage] = useState(1)

  const handleSort = (field) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDirection('asc')
    }
  }

  // Sorted and paginated data
  const processedData = useMemo(() => {
    let result = [...data]

    if (sortField) {
      result.sort((a, b) => {
        const valA = a[sortField]
        const valB = b[sortField]
        if (valA === valB) return 0
        if (valA === null || valA === undefined) return 1
        if (valB === null || valB === undefined) return -1
        
        if (typeof valA === 'number' && typeof valB === 'number') {
          return sortDirection === 'asc' ? valA - valB : valB - valA
        }
        return sortDirection === 'asc'
          ? String(valA).localeCompare(String(valB))
          : String(valB).localeCompare(String(valA))
      })
    }

    return result
  }, [data, sortField, sortDirection])

  const totalPages = Math.ceil(processedData.length / pageSize) || 1
  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return processedData.slice(start, start + pageSize)
  }, [processedData, currentPage, pageSize])

  const handleExportCsv = () => {
    if (!data.length) return
    downloadCsv(data, exportFilename)
  }

  return (
    <div className={`space-y-3 ${className}`}>
      {/* Table Toolbar */}
      {showExport && data.length > 0 && (
        <div className="flex justify-end no-print">
          <Button
            variant="outline"
            size="sm"
            onClick={handleExportCsv}
            icon={Download}
          >
            Export CSV
          </Button>
        </div>
      )}

      {/* Table Container */}
      <div className="overflow-x-auto rounded-xl border border-slate-200 dark:border-waypoint-darkBorder bg-white dark:bg-waypoint-darkSurface shadow-sm">
        <table className="w-full text-left text-sm text-slate-700 dark:text-slate-300">
          <thead className="bg-slate-50 dark:bg-waypoint-darkSubdued/80 text-xs uppercase tracking-wider font-semibold text-slate-500 dark:text-waypoint-darkMuted border-b border-slate-200 dark:border-waypoint-darkBorder">
            <tr>
              {columns.map((col, idx) => (
                <th
                  key={col.key || idx}
                  className={`px-4 py-3.5 ${col.sortable ? 'cursor-pointer select-none hover:text-slate-900 dark:hover:text-white' : ''} ${col.headerClassName || ''}`}
                  onClick={() => col.sortable && handleSort(col.key)}
                >
                  <div className="flex items-center gap-1.5">
                    <span>{col.header}</span>
                    {col.sortable && (
                      <ArrowUpDown className="w-3 h-3 text-slate-400 opacity-60" />
                    )}
                  </div>
                </th>
              ))}
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-100 dark:divide-waypoint-darkBorder/60">
            {isLoading ? (
              // Loading Skeleton Rows
              Array.from({ length: pageSize > 5 ? 5 : pageSize }).map((_, idx) => (
                <tr key={idx} className="animate-pulse">
                  {columns.map((col, cIdx) => (
                    <td key={cIdx} className="px-4 py-3.5">
                      <div className="h-4 bg-slate-200 dark:bg-waypoint-darkBorder rounded w-3/4" />
                    </td>
                  ))}
                </tr>
              ))
            ) : paginatedData.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-12 text-center">
                  <div className="text-slate-400 dark:text-waypoint-darkMuted font-medium text-sm">
                    {emptyTitle}
                  </div>
                  <div className="text-xs text-slate-400 mt-1">
                    {emptyDescription}
                  </div>
                </td>
              </tr>
            ) : (
              paginatedData.map((row, rIdx) => (
                <tr
                  key={row.id || rIdx}
                  className="hover:bg-slate-50/80 dark:hover:bg-waypoint-darkSubdued/40 transition-colors"
                >
                  {columns.map((col, cIdx) => (
                    <td key={col.key || cIdx} className={`px-4 py-3.5 ${col.className || ''}`}>
                      {col.render ? col.render(row, rIdx) : row[col.key]}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination Footer */}
      {!isLoading && processedData.length > pageSize && (
        <div className="flex items-center justify-between px-2 pt-1 text-xs text-slate-500 dark:text-waypoint-darkMuted no-print">
          <div>
            Showing <span className="font-semibold text-slate-700 dark:text-slate-300">{(currentPage - 1) * pageSize + 1}</span> to{' '}
            <span className="font-semibold text-slate-700 dark:text-slate-300">
              {Math.min(currentPage * pageSize, processedData.length)}
            </span>{' '}
            of <span className="font-semibold text-slate-700 dark:text-slate-300">{processedData.length}</span> records
          </div>

          <div className="flex items-center gap-1.5">
            <Button
              variant="outline"
              size="sm"
              disabled={currentPage === 1}
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              icon={ChevronLeft}
            >
              Prev
            </Button>
            <span className="px-2 font-medium">
              Page {currentPage} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={currentPage === totalPages}
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              icon={ChevronRight}
            >
              Next
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
