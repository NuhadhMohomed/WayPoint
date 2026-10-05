import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from '../LoginPage';
import { RegisterPage } from '../RegisterPage';
import { NotFoundPage } from '../NotFoundPage';
import { OverviewPage } from '../OverviewPage';
import { useAuthStore } from '../../store/authStore';
import { apiClient } from '../../api/client';

// Mock apiClient
vi.mock('../../api/client', () => ({
  default: {
    post: vi.fn(),
    get: vi.fn(),
  },
  apiClient: {
    post: vi.fn(),
    get: vi.fn(),
  }
}));

describe('Authentication & Overview Pages', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      user: null,
      token: null,
      refreshToken: null,
      isAuthenticated: false,
    });
  });

  it('renders LoginPage with credentials inputs and login button', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByRole('heading', { name: /waypoint/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/operator email address/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/secure password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /authenticate & access noc/i })).toBeInTheDocument();
  });

  it('submits LoginPage with credentials and sets auth store on success', async () => {
    apiClient.post.mockResolvedValueOnce({
      token: 'jwt-mock-token',
      user: { id: 'usr-1', email: 'officer@waypoint.lk', role: 'TransitManager', fullName: 'Operator 1' }
    });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const emailInput = screen.getByLabelText(/operator email address/i);
    const passwordInput = screen.getByLabelText(/secure password/i);
    const submitBtn = screen.getByRole('button', { name: /authenticate & access noc/i });

    fireEvent.change(emailInput, { target: { value: 'officer@waypoint.lk' } });
    fireEvent.change(passwordInput, { target: { value: 'Password123!' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith('/auth/login', {
        email: 'officer@waypoint.lk',
        password: 'Password123!'
      });
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
      expect(useAuthStore.getState().user.email).toBe('officer@waypoint.lk');
    });
  });

  it('renders RegisterPage with registration fields and submit button', () => {
    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>
    );

    expect(screen.getByRole('heading', { name: /create waypoint account/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/full name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/mobile number/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /register passenger account/i })).toBeInTheDocument();
  });

  it('renders NotFoundPage with 404 code and return button', () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>
    );

    expect(screen.getByText('404')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /corridor not found/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /return to operations cockpit/i })).toBeInTheDocument();
  });

  it('renders OverviewPage with live operational metrics and corridor status table', () => {
    useAuthStore.setState({
      user: { fullName: 'Nuhadh Admin', role: 'TransitManager', email: 'admin@waypoint.lk' },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter>
        <OverviewPage />
      </MemoryRouter>
    );

    expect(screen.getByRole('heading', { name: /transit operations cockpit/i })).toBeInTheDocument();
    expect(screen.getByText(/active trips in-transit/i)).toBeInTheDocument();
    expect(screen.getByText(/fleet availability rate/i)).toBeInTheDocument();
    expect(screen.getByText(/on-time dispatch rate/i)).toBeInTheDocument();
    expect(screen.getByText(/live corridor operations status/i)).toBeInTheDocument();
  });
});
