/**
 * Client-Side CSV Exporter Utility
 * Converts arrays of objects into formatted CSV and triggers browser download.
 *
 * @param {Array<Object>} data - Array of row objects
 * @param {string} filename - Download file name
 * @param {boolean} [triggerDownload=true] - Whether to trigger browser download
 * @returns {string} The generated CSV string
 */
export function downloadCsv(data, filename = 'export.csv', triggerDownload = true) {
  if (!data || !data.length) return ''

  const headers = Object.keys(data[0])
  const csvRows = []

  // Add header row
  csvRows.push(headers.join(','))

  // Add data rows
  for (const row of data) {
    const values = headers.map((header) => {
      const val = row[header]
      if (val === null || val === undefined) return ''
      const stringVal = typeof val === 'object' ? JSON.stringify(val) : String(val)
      // Escape commas, quotes, and newlines
      if (stringVal.includes(',') || stringVal.includes('"') || stringVal.includes('\n')) {
        return `"${stringVal.replace(/"/g, '""')}"`
      }
      return stringVal
    })
    csvRows.push(values.join(','))
  }

  const csvString = csvRows.join('\n')

  if (triggerDownload && typeof window !== 'undefined') {
    const blob = new Blob([csvString], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.setAttribute('href', url)
    link.setAttribute('download', filename.endsWith('.csv') ? filename : `${filename}.csv`)
    link.style.visibility = 'hidden'
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  }

  return csvString
}
