import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { RouteManagerPage } from '../RouteManagerPage'
import { journeyApi } from '../journeyApi'

// Mock journeyApi
vi.mock('../journeyApi', () => ({
  journeyApi: {
    getRoutes: vi.fn(),
    createRoute: vi.fn(),
    getRouteById: vi.fn(),
  },
}))

describe('RouteManagerPage (WEB-02)', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    journeyApi.getRoutes.mockResolvedValue([
      {
        id: 'r1',
        routeNumber: 'EX-08',
        originCity: 'Colombo',
        destinationCity: 'Ella',
        estimatedDurationMinutes: 360,
        isActive: true,
        stops: [
          { id: 's1', stopName: 'Colombo Bastian Hill', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
          { id: 's2', stopName: 'Ella Town Terminal', sequenceOrder: 2, arrivalOffsetMinutes: 360, distanceFromOriginKm: 215 },
        ],
      },
      {
        id: 'r2',
        routeNumber: 'RT-01',
        originCity: 'Colombo',
        destinationCity: 'Kandy',
        estimatedDurationMinutes: 195,
        isActive: true,
        stops: [
          { id: 'k1', stopName: 'Colombo Central Super', sequenceOrder: 1, arrivalOffsetMinutes: 0, distanceFromOriginKm: 0 },
          { id: 'k2', stopName: 'Kandy Goods Shed', sequenceOrder: 2, arrivalOffsetMinutes: 195, distanceFromOriginKm: 115 },
        ],
      },
    ])
  })

  it('renders route catalogue with route codes and destination pairs', async () => {
    render(<RouteManagerPage />)

    await waitFor(() => {
      expect(screen.getByText('EX-08')).toBeInTheDocument()
      expect(screen.getByText('RT-01')).toBeInTheDocument()
      expect(screen.getByText('Ella')).toBeInTheDocument()
      expect(screen.getByText('Kandy')).toBeInTheDocument()
    })
  })

  it('filters routes when user enters search terms', async () => {
    render(<RouteManagerPage />)

    await waitFor(() => {
      expect(screen.getByText('EX-08')).toBeInTheDocument()
    })

    const searchInput = screen.getByPlaceholderText(/Filter by route code/i)
    fireEvent.change(searchInput, { target: { value: 'Ella' } })

    expect(screen.getByText('EX-08')).toBeInTheDocument()
    expect(screen.queryByText('RT-01')).not.toBeInTheDocument()
  })

  it('opens Add Route modal when clicking Create Intercity Route button', async () => {
    render(<RouteManagerPage />)

    await waitFor(() => {
      expect(screen.getByText('Create Intercity Route')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Create Intercity Route'))

    expect(screen.getByText('Create Intercity Route (WEB-02)')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('e.g. EX-09')).toBeInTheDocument()
  })

  it('opens intermediate stop inspection drawer when clicking Inspect Stops', async () => {
    render(<RouteManagerPage />)

    await waitFor(() => {
      expect(screen.getAllByText('Inspect Stops')[0]).toBeInTheDocument()
    })

    fireEvent.click(screen.getAllByText('Inspect Stops')[0])

    await waitFor(() => {
      expect(screen.getByText('Colombo to Ella')).toBeInTheDocument()
      expect(screen.getByText('Colombo Bastian Hill')).toBeInTheDocument()
      expect(screen.getByText('Ella Town Terminal')).toBeInTheDocument()
    })
  })
})

