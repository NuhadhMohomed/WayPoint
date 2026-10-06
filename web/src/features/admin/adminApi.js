import { apiClient } from '@/api/client'

/**
 * System Administration & User Governance API Client (API §4.2 & §4.3)
 */
export const adminApi = {
  /**
   * Retrieves a paginated list of users with search and filter parameters.
   */
  getUsers: async (params = {}) => {
    const res = await apiClient.get('/users', { params })
    return res.data
  },

  /**
   * Retrieves a single user profile with operational metadata.
   */
  getUserById: async (id) => {
    const res = await apiClient.get(`/users/${id}`)
    return res.data
  },

  /**
   * Reassigns a user's role claim (BR-ADMIN-001, BR-ADMIN-002, BR-AUDIT-001).
   */
  updateUserRole: async (id, data) => {
    const res = await apiClient.put(`/users/${id}/role`, data)
    return res.data
  },

  /**
   * Updates user status or unlocks an account (BR-AUTH-002, BR-ADMIN-001).
   */
  updateUserStatus: async (id, data) => {
    const res = await apiClient.patch(`/users/${id}/status`, data)
    return res.data
  },

  /**
   * Provisions a new user account with pre-assigned role and profiles.
   */
  createUser: async (data) => {
    const res = await apiClient.post('/users', data)
    return res.data
  },

  /**
   * Retrieves available system roles with counts and descriptions.
   */
  getRoles: async () => {
    const res = await apiClient.get('/users/roles')
    return res.data
  },

  /**
   * Retrieves immutable audit logs for security oversight (BR-AUDIT-001).
   */
  getAuditLogs: async (params = {}) => {
    const res = await apiClient.get('/dev/audit-logs', { params })
    return res.data
  }
}
