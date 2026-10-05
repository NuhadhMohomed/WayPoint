import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '../../store/authStore';
import { apiClient } from '../client';

describe('API Client & Auth Store', () => {
  beforeEach(() => {
    useAuthStore.getState().logout();
  });

  it('stores JWT token and computes role permissions correctly', () => {
    useAuthStore.getState().setAuth('mock-token-xyz', {
      id: 'usr-1',
      email: 'manager@waypoint.lk',
      role: 'TransportManager'
    });

    const state = useAuthStore.getState();
    expect(state.token).toBe('mock-token-xyz');
    expect(state.isManager).toBe(true);
    expect(state.isAdmin).toBe(false);
  });

  it('clears token on logout', () => {
    useAuthStore.getState().setAuth('mock-token-xyz', {
      id: 'usr-1',
      email: 'manager@waypoint.lk',
      role: 'TransportManager'
    });
    useAuthStore.getState().logout();
    const state = useAuthStore.getState();
    expect(state.token).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });
});
