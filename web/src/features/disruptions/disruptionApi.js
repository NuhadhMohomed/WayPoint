import { apiClient } from '@/api/client'

export const disruptionApi = {
  logDisruption: async (disruptionData) => {
    const res = await apiClient.post('/disruptions', disruptionData)
    return res.data
  },
  getDisruptions: async (params) => {
    const res = await apiClient.get('/disruptions', { params })
    return res.data
  },
  getDisruptionImpact: async (disruptionId) => {
    const res = await apiClient.get(`/disruptions/${disruptionId}/impact`)
    return res.data
  },
  generateRebookingProposal: async (disruptionId) => {
    const res = await apiClient.post('/rebooking/generate-proposal', { disruptionId })
    return res.data
  },
  getPendingApprovals: async () => {
    const res = await apiClient.get('/approvals/pending')
    return res.data
  },
  submitApprovalDecision: async (proposalId, decisionData) => {
    const res = await apiClient.post(`/approvals/${proposalId}/decision`, decisionData)
    return res.data
  },
  getAlerts: async () => {
    const res = await apiClient.get('/alerts')
    return res.data
  },
  broadcastAlert: async (alertData) => {
    const res = await apiClient.post('/alerts', alertData)
    return res.data
  },
}
