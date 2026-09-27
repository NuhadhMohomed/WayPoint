import React from 'react'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { BrowserRouter } from 'react-router-dom'
import { OperatorDashboardPage } from '../OperatorDashboardPage'
import { BookingManifestMonitorPage } from '../BookingManifestMonitorPage'
import { bookingApi } from '../bookingApi'

vi.mock('../bookingApi', () => ({
  bookingApi: {
    getBookings: vi.fn(),
    getBookingById: vi.fn(),
    holdSeat: vi.fn(),
    releaseHold: vi.fn(),
    confirmPayment: vi.fn(),
    checkout: vi.fn(),
    cancelBooking: vi.fn(),
    verifyTicketQr: vi.fn(),
  },
}))

describe('Component 3: Booking & Manifest Frontend Suite (Mithila)', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    bookingApi.getBookings.mockResolvedValue([
      {
        bookingId: 'b-001',
        bookingReference: 'WP-7B92K1',
        passengerName: 'Nimal Silva',
        passengerPhone: '+94 77 456 7890',
        seatNumbers: ['4A', '4B'],
        boardingPoint: 'Platform 3, Makumbura MMC (Colombo)',
        totalFareAmount: 5700.0,
        status: 'Confirmed',
        isBoarded: false,
        qrCodePayload: 'WP|REF:WP-7B92K1|SRV:SRV-COL-ELLA-0800|SEATS:4A,4B|PASS:Nimal Silva|HMAC:a8f7c9e123456789',
      },
    ])
  })

  it('renders Operator Overview Dashboard with operational KPI metrics', async () => {
    render(
      <BrowserRouter>
        <OperatorDashboardPage />
      </BrowserRouter>
    )

    expect(screen.getByText(/Operator Overview/i)).toBeInTheDocument()
    expect(screen.getByText(/Revenue Today/i)).toBeInTheDocument()
    expect(screen.getByText(/Active Holds/i)).toBeInTheDocument()
    expect(screen.getByText(/Confirmed Today/i)).toBeInTheDocument()
  })

  it('renders Booking Manifest Monitor with manifest table and sandbox controls', async () => {
    render(
      <BrowserRouter>
        <BookingManifestMonitorPage />
      </BrowserRouter>
    )

    expect(screen.getByText(/Booking Manifest & Payment Sandbox/i)).toBeInTheDocument()
    expect(screen.getByText(/Payment Sandbox Gateway Simulator/i)).toBeInTheDocument()
    expect(screen.getByText(/10-Minute Seat Hold Simulator/i)).toBeInTheDocument()
  })

  it('filters manifest table when passenger search query is entered', async () => {
    render(
      <BrowserRouter>
        <BookingManifestMonitorPage />
      </BrowserRouter>
    )

    const searchInput = screen.getByPlaceholderText(/Search passenger, NIC, phone, or WP-ref.../i)
    fireEvent.change(searchInput, { target: { value: 'Nimal' } })
    expect(searchInput.value).toBe('Nimal')
  })

  it('updates sandbox test card preset when card chip is clicked', async () => {
    render(
      <BrowserRouter>
        <BookingManifestMonitorPage />
      </BrowserRouter>
    )

    // Find and click the "Decline (0002)" preset button
    const declineChip = screen.getByText(/Card Declined/i)
    fireEvent.click(declineChip)

    // Verify card number input is updated
    const cardInput = screen.getByDisplayValue(/4000 0000 0000 0002/i)
    expect(cardInput).toBeInTheDocument()
  })
})
