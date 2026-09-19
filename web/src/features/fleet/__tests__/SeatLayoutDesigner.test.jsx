import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { SeatLayoutDesignerPage } from '../SeatLayoutDesignerPage'
import { fleetApi } from '../fleetApi'

// Mock the fleetApi
vi.mock('../fleetApi', () => ({
  fleetApi: {
    getSeatLayouts: vi.fn(),
    createSeatLayout: vi.fn(),
    getSeatLayoutById: vi.fn()
  }
}))

describe('SeatLayoutDesignerPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    fleetApi.getSeatLayouts.mockResolvedValue([])
  })

  it('renders templates list initially', async () => {
    render(<SeatLayoutDesignerPage />)
    
    // Check loading state
    expect(screen.getByRole('heading', { name: /Seat Layout Templates/i })).toBeInTheDocument()
    
    await waitFor(() => {
      expect(screen.getByText(/No seat layouts defined yet/i)).toBeInTheDocument()
    })
  })

  it('opens designer when clicking New Layout', async () => {
    render(<SeatLayoutDesignerPage />)
    
    // Wait for initial load
    await waitFor(() => {
      expect(screen.queryByText(/No seat layouts defined yet/i)).toBeInTheDocument()
    })
    
    // Click new layout button
    fireEvent.click(screen.getByRole('button', { name: /New Layout/i }))
    
    expect(screen.getByRole('heading', { name: /Visual Seat Layout Designer/i })).toBeInTheDocument()
    expect(screen.getByText('Total Seats')).toBeInTheDocument()
    // 10x4 = 40 default seats
    expect(screen.getAllByText('40')[0]).toBeInTheDocument()
  })

  it('changes grid size when rows and columns inputs are modified', async () => {
    render(<SeatLayoutDesignerPage />)
    await waitFor(() => screen.getByRole('button', { name: /New Layout/i }))
    fireEvent.click(screen.getByRole('button', { name: /New Layout/i }))
    
    const rowInput = screen.getByDisplayValue('10')
    const colInput = screen.getByDisplayValue('4')
    
    fireEvent.change(rowInput, { target: { value: '5' } })
    fireEvent.change(colInput, { target: { value: '2' } })
    
    // 5x2 = 10 seats
    expect(screen.getAllByText('10')[0]).toBeInTheDocument()
  })

  it('cycles seat types when clicking a seat', async () => {
    render(<SeatLayoutDesignerPage />)
    await waitFor(() => screen.getByRole('button', { name: /New Layout/i }))
    fireEvent.click(screen.getByRole('button', { name: /New Layout/i }))
    
    // Change to a small 1x1 grid for easy testing
    fireEvent.change(screen.getByDisplayValue('10'), { target: { value: '1' } })
    fireEvent.change(screen.getByDisplayValue('4'), { target: { value: '1' } })
    
    // Default is Standard (shows 1A)
    const seat = screen.getByTitle('Row 1, Col 1 — Standard (click to change)')
    expect(seat).toHaveTextContent('1A')
    
    // Click 1: Standard -> Window
    fireEvent.click(seat)
    expect(screen.getByTitle('Row 1, Col 1 — Window (click to change)')).toHaveTextContent('1A')
    
    // Click 2: Window -> VIP
    fireEvent.click(seat)
    expect(screen.getByTitle('Row 1, Col 1 — VIP (click to change)')).toHaveTextContent('1A')
    
    // Click 3: VIP -> empty
    fireEvent.click(seat)
    expect(screen.getByTitle('Row 1, Col 1 — empty (click to change)')).toHaveTextContent('')
  })

  it('calls createSeatLayout with correct data when saving', async () => {
    fleetApi.createSeatLayout.mockResolvedValue({ id: '123' })
    
    render(<SeatLayoutDesignerPage />)
    await waitFor(() => screen.getByRole('button', { name: /New Layout/i }))
    fireEvent.click(screen.getByRole('button', { name: /New Layout/i }))
    
    // Set 1x1 grid
    fireEvent.change(screen.getByDisplayValue('10'), { target: { value: '1' } })
    fireEvent.change(screen.getByDisplayValue('4'), { target: { value: '1' } })
    
    // Type name
    const nameInput = screen.getByPlaceholderText('e.g. Standard 2×2 (40 seats)')
    fireEvent.change(nameInput, { target: { value: 'Mini Bus' } })
    
    // Click save
    fireEvent.click(screen.getByRole('button', { name: /Save Layout/i }))
    
    await waitFor(() => {
      expect(fleetApi.createSeatLayout).toHaveBeenCalledWith({
        name: 'Mini Bus',
        totalRows: 1,
        totalColumns: 1,
        seats: [
          {
            seatNumber: '1A',
            rowIndex: 0,
            columnIndex: 0,
            seatClass: 'Standard'
          }
        ]
      })
    })
    
    // Success toast
    await waitFor(() => {
      expect(screen.getByText('Layout "Mini Bus" created with 1 seats!')).toBeInTheDocument()
    })
  })
})
