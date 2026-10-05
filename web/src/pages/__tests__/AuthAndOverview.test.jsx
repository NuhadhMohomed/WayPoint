import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { BrowserRouter } from 'react-router-dom'
import { LoginPage } from '../LoginPage'
import { RegisterPage } from '../RegisterPage'
import { OverviewPage } from '../OverviewPage'

vi.mock('../../api/client', () => ({
  authApi: {
    login: vi.fn(),
    register: vi.fn(),
  },
}))

vi.mock('../../store/authStore', () => ({
  useAuthStore: () => ({
    user: { fullName: 'Sethum Lead', role: 'Admin' },
    setAuth: vi.fn(),
  }),
}))

describe('Task 4: Auth & Overview Cockpit Suite', () => {
  describe('LoginPage', () => {
    it('renders login form with quick fill demo account chips', () => {
      render(
        <BrowserRouter>
          <LoginPage />
        </BrowserRouter>
      )

      expect(screen.getByLabelText(/Operator Email Address/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/Secure Password/i)).toBeInTheDocument()
      expect(screen.getByText('Manager')).toBeInTheDocument()
      expect(screen.getByText('Operator')).toBeInTheDocument()

      // Click Manager demo fill
      fireEvent.click(screen.getByText('Manager'))
      expect(screen.getByDisplayValue('manager@waypoint.lk')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Password123!')).toBeInTheDocument()
    })

    it('toggles password visibility when show/hide button is clicked', () => {
      render(
        <BrowserRouter>
          <LoginPage />
        </BrowserRouter>
      )

      const pwdInput = screen.getByLabelText(/Secure Password/i)
      expect(pwdInput).toHaveAttribute('type', 'password')

      const toggleBtn = screen.getByRole('button', { name: /Show/i })
      fireEvent.click(toggleBtn)
      expect(pwdInput).toHaveAttribute('type', 'text')
    })
  })

  describe('RegisterPage', () => {
    it('renders full name, email, phone number, and password fields', () => {
      render(
        <BrowserRouter>
          <RegisterPage />
        </BrowserRouter>
      )

      expect(screen.getByLabelText(/Full Name/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/Email Address/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/Mobile Number/i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /Register Passenger Account/i })).toBeInTheDocument()
    })
  })

  describe('OverviewPage', () => {
    it('renders telematics KPI metrics and corridor operational telemetry', () => {
      render(
        <BrowserRouter>
          <OverviewPage />
        </BrowserRouter>
      )

      expect(screen.getByText(/Transit Operations Cockpit/i)).toBeInTheDocument()
      expect(screen.getByText(/Active Trips In-Transit/i)).toBeInTheDocument()
      expect(screen.getByText(/On-Time Dispatch Rate/i)).toBeInTheDocument()
      expect(screen.getByText(/Live Corridor Operations Status/i)).toBeInTheDocument()
      expect(screen.getByText('Southern Coastal Expressway')).toBeInTheDocument()
    })
  })
})
