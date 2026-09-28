import { apiClient } from '@/api/client'

export const disruptionApi = {
  // Disruptions
  logDisruption: async (disruptionData) => {
    const res = await apiClient.post('/disruptions', disruptionData)
    return res.data
  },
  getDisruptions: async (params) => {
    const res = await apiClient.get('/disruptions', { params })
    return res.data
  },
  getDisruptionById: async (disruptionId) => {
    const res = await apiClient.get(`/disruptions/${disruptionId}`)
    return res.data
  },
  getDisruptionImpact: async (disruptionId) => {
    const res = await apiClient.get(`/disruptions/${disruptionId}/impact`)
    return res.data
  },

  // Rebooking
  generateRebookingProposal: async (proposalData) => {
    const res = await apiClient.post('/rebooking/generate-proposal', proposalData)
    return res.data
  },
  getProposalsByDisruption: async (disruptionId) => {
    const res = await apiClient.get(`/rebooking/disruption/${disruptionId}`)
    return res.data
  },
  executeRebooking: async (proposalId) => {
    const res = await apiClient.post(`/rebooking/${proposalId}/execute`)
    return res.data
  },

  // Approvals (Transport Manager Authority - BR-APPROVAL-001)
  getPendingApprovals: async () => {
    const res = await apiClient.get('/approvals/pending')
    return res.data
  },
  submitApprovalDecision: async (proposalId, decisionData) => {
    const res = await apiClient.post(`/approvals/${proposalId}/decision`, decisionData)
    return res.data
  },

  // Service Alerts
  getAlerts: async () => {
    const res = await apiClient.get('/alerts')
    return res.data
  },
  broadcastAlert: async (alertData) => {
    const res = await apiClient.post('/alerts', alertData)
    return res.data
  },

  // AI Workflow Observability & Traces
  getAiWorkflows: async (params) => {
    const res = await apiClient.get('/ai/workflows', { params })
    return res.data
  },
  getAiWorkflowById: async (workflowId) => {
    const res = await apiClient.get(`/ai/workflows/${workflowId}`)
    return res.data
  },

  // Helpers
  getServices: async () => {
    const res = await apiClient.get('/dev/services')
    return res.data
  },
  getAuditLogs: async (params) => {
    const res = await apiClient.get('/dev/audit-logs', { params })
    return res.data
  },
}
