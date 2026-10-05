import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { 
  Button, 
  TransitBadge, 
  EmptyState, 
  ErrorState, 
  Modal, 
  DataTable, 
  OfflineBanner,
  JsonDiffViewer 
} from '../index'
import { downloadCsv } from '../../../lib/csvExport'

describe('Core UI Component Primitives', () => {
  describe('Button component', () => {
    it('renders with Spring Green styling and high contrast text', () => {
      render(<Button variant="primary">Confirm Booking</Button>)
      const btn = screen.getByRole('button', { name: 'Confirm Booking' })
      expect(btn).toBeInTheDocument()
      expect(btn.className).toContain('bg-waypoint-primary')
      expect(btn.className).toContain('text-waypoint-onPrimary')
    })

    it('displays loading spinner and disables button when isLoading is true', () => {
      render(<Button isLoading>Processing</Button>)
      const btn = screen.getByRole('button')
      expect(btn).toBeDisabled()
      expect(btn.querySelector('.animate-spin')).toBeInTheDocument()
    })

    it('renders secondary amber and outline variants correctly', () => {
      render(<Button variant="secondary">Hold Seat</Button>)
      expect(screen.getByRole('button', { name: 'Hold Seat' }).className).toContain('bg-waypoint-amber')
    })
  })

  describe('TransitBadge component', () => {
    it('renders Available with green styling', () => {
      render(<TransitBadge status="Available" />)
      expect(screen.getByText('Available')).toBeInTheDocument()
      expect(screen.getByText('Available').closest('span').className).toContain('text-emerald-400')
    })

    it('renders Held with amber styling', () => {
      render(<TransitBadge status="Held" label="10m Lock" />)
      expect(screen.getByText('10m Lock')).toBeInTheDocument()
      expect(screen.getByText('10m Lock').closest('span').className).toContain('text-amber-400')
    })

    it('renders Disrupted with crimson alert styling', () => {
      render(<TransitBadge status="Disrupted" />)
      expect(screen.getByText('Disrupted')).toBeInTheDocument()
      expect(screen.getByText('Disrupted').closest('span').className).toContain('text-red-400')
    })
  })

  describe('EmptyState component', () => {
    it('renders title, description and optional action button', () => {
      const handleAction = vi.fn()
      render(
        <EmptyState 
          title="No routes found"
          description="Try adjusting your corridor filters."
          actionLabel="Clear Filters"
          onAction={handleAction}
        />
      )
      expect(screen.getByText('No routes found')).toBeInTheDocument()
      expect(screen.getByText('Try adjusting your corridor filters.')).toBeInTheDocument()
      
      const actionBtn = screen.getByRole('button', { name: 'Clear Filters' })
      fireEvent.click(actionBtn)
      expect(handleAction).toHaveBeenCalledTimes(1)
    })
  })

  describe('ErrorState component', () => {
    it('renders error message and triggers retry callback', () => {
      const handleRetry = vi.fn()
      render(
        <ErrorState 
          message="Failed to connect to Railway PostgreSQL database"
          onRetry={handleRetry}
        />
      )
      expect(screen.getByText('Failed to connect to Railway PostgreSQL database')).toBeInTheDocument()
      
      const retryBtn = screen.getByRole('button', { name: /retry|try again/i })
      fireEvent.click(retryBtn)
      expect(handleRetry).toHaveBeenCalledTimes(1)
    })
  })

  describe('Modal component', () => {
    it('renders when isOpen is true and dismisses on Escape key', () => {
      const handleClose = vi.fn()
      render(
        <Modal isOpen={true} onClose={handleClose} title="Add New Corridor">
          <div>Modal Body Content</div>
        </Modal>
      )
      expect(screen.getByText('Add New Corridor')).toBeInTheDocument()
      expect(screen.getByText('Modal Body Content')).toBeInTheDocument()

      fireEvent.keyDown(document, { key: 'Escape', code: 'Escape' })
      expect(handleClose).toHaveBeenCalledTimes(1)
    })
  })

  describe('CSV Export Utility', () => {
    it('formats tabular data into downloadable CSV string', () => {
      const data = [
        { route: 'EX-08', from: 'Colombo', to: 'Ella', fare: 2400 },
        { route: 'RT-01', from: 'Colombo', to: 'Kandy', fare: 850 },
      ]
      const csv = downloadCsv(data, 'routes.csv', false)
      expect(csv).toContain('route,from,to,fare')
      expect(csv).toContain('EX-08,Colombo,Ella,2400')
      expect(csv).toContain('RT-01,Colombo,Kandy,850')
    })
  })
})
