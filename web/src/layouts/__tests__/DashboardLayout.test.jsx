import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { BrowserRouter } from 'react-router-dom'
import { DashboardLayout } from '../DashboardLayout'

// Mock devApi and themeStore
vi.mock('../../api/client', () => ({
  devApi: {
    seedDatabase: vi.fn().mockResolvedValue({ data: { message: 'Database seeded' } }),
  },
}))

vi.mock('../../store/authStore', () => ({
  useAuthStore: () => ({
    user: { fullName: 'Sethum Transit', role: 'Admin' },
    logout: vi.fn(),
  }),
}))

describe('DashboardLayout Component', () => {
  it('renders WayPoint NOC brand, navigation items, and SLST clock', () => {
    render(
      <BrowserRouter>
        <DashboardLayout />
      </BrowserRouter>
    )

    // Check brand
    expect(screen.getAllByText('WayPoint')[0]).toBeInTheDocument()
    expect(screen.getAllByText('NOC')[0]).toBeInTheDocument()

    // Check SLST clock
    expect(screen.getByText('SLST')).toBeInTheDocument()

    // Check Navigation
    expect(screen.getByText('Overview Cockpit')).toBeInTheDocument()
    expect(screen.getByText('Routes & Timetables')).toBeInTheDocument()
  })

  it('opens Command Palette when jump button is clicked', () => {
    render(
      <BrowserRouter>
        <DashboardLayout />
      </BrowserRouter>
    )

    const searchBtn = screen.getByTitle('Global Command Palette (Ctrl+K)')
    fireEvent.click(searchBtn)

    expect(screen.getByPlaceholderText(/Type a command or transit corridor/i)).toBeInTheDocument()
  })

  it('renders density and theme toggles', () => {
    render(
      <BrowserRouter>
        <DashboardLayout />
      </BrowserRouter>
    )

    const densityBtn = screen.getByLabelText('Toggle compact density')
    const themeBtn = screen.getByLabelText('Toggle color theme')

    expect(densityBtn).toBeInTheDocument()
    expect(themeBtn).toBeInTheDocument()
  })
})
