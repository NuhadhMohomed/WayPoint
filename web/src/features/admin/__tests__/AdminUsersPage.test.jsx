import React from 'react'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { BrowserRouter } from 'react-router-dom'
import { AdminUsersPage } from '../AdminUsersPage'
import { adminApi } from '../adminApi'

vi.mock('../adminApi', () => ({
  adminApi: {
    getUsers: vi.fn(),
    getUserById: vi.fn(),
    updateUserRole: vi.fn(),
    updateUserStatus: vi.fn(),
    createUser: vi.fn(),
    getRoles: vi.fn(),
    getAuditLogs: vi.fn()
  }
}))

describe('Admin Users & Role Governance Frontend Suite', () => {
  const mockUsers = [
    {
      id: 'usr-admin-1',
      fullName: 'System Administrator',
      email: 'admin@waypoint.lk',
      phoneNumber: '+94771234567',
      role: 'Admin',
      isActive: true,
      failedLoginAttempts: 0,
      lockedUntil: null,
      isLocked: false,
      profileMetadata: 'Super User',
      createdAt: '2026-09-01T00:00:00Z'
    },
    {
      id: 'usr-op-1',
      fullName: 'Transit Operator',
      email: 'operator@waypoint.lk',
      phoneNumber: '+94772345678',
      role: 'Operator',
      isActive: true,
      failedLoginAttempts: 0,
      lockedUntil: null,
      isLocked: false,
      profileMetadata: 'OP-SLTB-WEST • Western Province',
      createdAt: '2026-09-02T00:00:00Z'
    },
    {
      id: 'usr-pass-locked',
      fullName: 'Nimal Silva',
      email: 'nimal@waypoint.lk',
      phoneNumber: '+94773456789',
      role: 'Passenger',
      isActive: true,
      failedLoginAttempts: 5,
      lockedUntil: '2026-10-06T12:00:00Z',
      isLocked: true,
      profileMetadata: 'NIC: 200012345678',
      createdAt: '2026-09-03T00:00:00Z'
    }
  ]

  beforeEach(() => {
    vi.resetAllMocks()
    adminApi.getUsers.mockResolvedValue({
      items: mockUsers,
      totalCount: 3,
      pageNumber: 1,
      pageSize: 10
    })
  })

  it('renders Admin Users Page with telemetry metrics and user table rows', async () => {
    render(
      <BrowserRouter>
        <AdminUsersPage />
      </BrowserRouter>
    )

    // Verify Telemetry card values
    await waitFor(() => {
      expect(screen.getByText('Total Registered Users')).toBeInTheDocument()
      expect(screen.getByText('Active Administrators')).toBeInTheDocument()
      expect(screen.getByText('Operational Personnel')).toBeInTheDocument()
      expect(screen.getByText('Restricted / Locked')).toBeInTheDocument()
    })

    // Verify User rows rendered
    expect(screen.getByText('System Administrator')).toBeInTheDocument()
    expect(screen.getByText('Transit Operator')).toBeInTheDocument()
    expect(screen.getByText('Nimal Silva')).toBeInTheDocument()

    // Verify Locked badge for Nimal
    expect(screen.getByText('Locked (BR-AUTH-002)')).toBeInTheDocument()
  })

  it('filters user table when search input is typed', async () => {
    render(
      <BrowserRouter>
        <AdminUsersPage />
      </BrowserRouter>
    )

    await waitFor(() => {
      expect(screen.getByText('System Administrator')).toBeInTheDocument()
    })

    const searchInput = screen.getByPlaceholderText('Search by name, email, or phone...')
    fireEvent.change(searchInput, { target: { value: 'Nimal' } })

    expect(screen.getByText('Nimal Silva')).toBeInTheDocument()
    expect(screen.queryByText('System Administrator')).not.toBeInTheDocument()
    expect(screen.queryByText('Transit Operator')).not.toBeInTheDocument()
  })

  it('filters user table by role dropdown selector', async () => {
    render(
      <BrowserRouter>
        <AdminUsersPage />
      </BrowserRouter>
    )

    await waitFor(() => {
      expect(screen.getByText('Transit Operator')).toBeInTheDocument()
    })

    const roleSelect = screen.getByDisplayValue('All Roles')
    fireEvent.change(roleSelect, { target: { value: 'Operator' } })

    expect(screen.getByText('Transit Operator')).toBeInTheDocument()
    expect(screen.queryByText('System Administrator')).not.toBeInTheDocument()
    expect(screen.queryByText('Nimal Silva')).not.toBeInTheDocument()
  })

  it('executes quick account unlock for locked user', async () => {
    adminApi.updateUserStatus.mockResolvedValue({
      ...mockUsers[2],
      failedLoginAttempts: 0,
      lockedUntil: null,
      isLocked: false
    })

    render(
      <BrowserRouter>
        <AdminUsersPage />
      </BrowserRouter>
    )

    await waitFor(() => {
      expect(screen.getByText('Locked (BR-AUTH-002)')).toBeInTheDocument()
    })

    const unlockBtn = screen.getByRole('button', { name: /unlock/i })
    fireEvent.click(unlockBtn)

    await waitFor(() => {
      expect(adminApi.updateUserStatus).toHaveBeenCalledWith('usr-pass-locked', expect.objectContaining({
        unlockAccount: true
      }))
    })
  })

  it('opens ChangeRoleModal when Edit Role button is clicked', async () => {
    render(
      <BrowserRouter>
        <AdminUsersPage />
      </BrowserRouter>
    )

    await waitFor(() => {
      expect(screen.getByText('Nimal Silva')).toBeInTheDocument()
    })

    const editRoleButtons = screen.getAllByRole('button', { name: /edit role/i })
    fireEvent.click(editRoleButtons[0]) // First user edit role

    await waitFor(() => {
      expect(screen.getByText('Reassign System Role')).toBeInTheDocument()
      expect(screen.getByText('Select New Authority Tier')).toBeInTheDocument()
    })
  })
})
