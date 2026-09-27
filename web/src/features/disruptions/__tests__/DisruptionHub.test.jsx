import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { BrowserRouter } from 'react-router-dom'
import { DisruptionIntakePage } from '../DisruptionIntakePage'
import { ManagerApprovalWorkbenchPage } from '../ManagerApprovalWorkbenchPage'
import { disruptionApi } from '../disruptionApi'

vi.mock('../disruptionApi', () => ({
  disruptionApi: {
    getDisruptions: vi.fn(),
    getServices: vi.fn(),
    logDisruption: vi.fn(),
    getDisruptionImpact: vi.fn(),
    generateRebookingProposal: vi.fn(),
    getPendingApprovals: vi.fn(),
    submitApprovalDecision: vi.fn(),
  },
}))

describe('Disruption & Rebooking Frontend Hub', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    disruptionApi.getDisruptions.mockResolvedValue([
      {
        id: 'disp-001',
        disruptedServiceId: 'svc-001',
        serviceNumber: 'NC-104',
        origin: 'Colombo Fort',
        destination: 'Kandy Goods Shed',
        reason: 'Engine breakdown near Kadawatha',
        severity: 'Critical',
        affectedPassengerCount: 42,
        status: 'Active',
        createdAt: '2026-09-26T08:00:00Z',
      },
    ])
    disruptionApi.getServices.mockResolvedValue([
      { id: 'svc-001', serviceNumber: 'NC-104', routeName: 'Colombo - Kandy' },
      { id: 'svc-002', serviceNumber: 'EX-501', routeName: 'Colombo - Galle' },
    ])
    disruptionApi.getPendingApprovals.mockResolvedValue([
      {
        rebookingProposalId: 'prop-001-uuid',
        disruptionCaseId: 'disp-001',
        disruptedServiceNumber: 'NC-104',
        replacementServiceNumber: 'EX-501',
        severity: 'Critical',
        affectedPassengerCount: 42,
        departureTimeShiftMinutes: 45,
        fareDelta: 0.0,
        status: 'PendingManagerApproval',
        isHighImpact: true,
      },
    ])
  })

  it('renders Disruption Intake Page with metrics and active cases', async () => {
    render(
      <BrowserRouter>
        <DisruptionIntakePage />
      </BrowserRouter>
    )

    expect(screen.getByText('Active Incidents')).toBeInTheDocument()
    expect(screen.getByText('Critical Disruptions')).toBeInTheDocument()
    expect(screen.getByText('Impacted Passengers')).toBeInTheDocument()
    
    await waitFor(() => {
      expect(screen.getByText(/Engine breakdown near Kadawatha/i)).toBeInTheDocument()
    })
  })

  it('renders Manager Approval Workbench with pending high-impact proposals', async () => {
    render(
      <BrowserRouter>
        <ManagerApprovalWorkbenchPage />
      </BrowserRouter>
    )

    expect(screen.getByText(/Manager Approval Boundary/i)).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.getByText(/Pending Approval Queue/i)).toBeInTheDocument()
      expect(screen.getByText(/Proposal #prop-001/i)).toBeInTheDocument()
      expect(screen.getByText(/Originating Disruption Case:/i)).toBeInTheDocument()
    })
  })
})
