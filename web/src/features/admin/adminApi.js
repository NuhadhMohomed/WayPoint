import { apiClient } from '@/api/client'

const unwrap = (res) => (res && res.data !== undefined && !res.items && !Array.isArray(res) ? res.data : res)

/**
 * System Administration & User Governance API Client (API §4.2 & §4.3)
 */
export const adminApi = {
  /**
   * Retrieves a paginated list of users with search and filter parameters.
   */
  getUsers: async (params = {}) => unwrap(await apiClient.get('/users', { params })),

  /**
   * Retrieves a single user profile with operational metadata.
   */
  getUserById: async (id) => unwrap(await apiClient.get(`/users/${id}`)),

  /**
   * Reassigns a user's role claim (BR-ADMIN-001, BR-ADMIN-002, BR-AUDIT-001).
   */
  updateUserRole: async (id, data) => unwrap(await apiClient.put(`/users/${id}/role`, data)),

  /**
   * Updates user status or unlocks an account (BR-AUTH-002, BR-ADMIN-001).
   */
  updateUserStatus: async (id, data) => unwrap(await apiClient.patch(`/users/${id}/status`, data)),

  /**
   * Provisions a new user account with pre-assigned role and profiles.
   */
  createUser: async (data) => unwrap(await apiClient.post('/users', data)),

  /**
   * Retrieves available system roles with counts and descriptions.
   */
  getRoles: async () => unwrap(await apiClient.get('/users/roles')),

  /**
   * Retrieves immutable audit logs for security oversight (BR-AUDIT-001).
   */
  getAuditLogs: async (params = {}) => unwrap(await apiClient.get('/dev/audit-logs', { params }))
}
