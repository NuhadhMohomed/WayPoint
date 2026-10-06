import { apiClient } from '@/api/client'

const unwrap = (res) => (res && res.data !== undefined && !res.items && !Array.isArray(res) ? res.data : res)

export const disruptionApi = {
  // Disruptions
  logDisruption: async (disruptionData) => unwrap(await apiClient.post('/disruptions', disruptionData)),
  getDisruptions: async (params) => unwrap(await apiClient.get('/disruptions', { params })),
  getDisruptionById: async (disruptionId) => unwrap(await apiClient.get(`/disruptions/${disruptionId}`)),
  getDisruptionImpact: async (disruptionId) => unwrap(await apiClient.get(`/disruptions/${disruptionId}/impact`)),

  // Rebooking
  generateRebookingProposal: async (proposalData) => unwrap(await apiClient.post('/rebooking/generate-proposal', proposalData)),
  getProposalsByDisruption: async (disruptionId) => unwrap(await apiClient.get(`/rebooking/disruption/${disruptionId}`)),
  executeRebooking: async (proposalId) => unwrap(await apiClient.post(`/rebooking/${proposalId}/execute`)),

  // Approvals (Transport Manager Authority - BR-APPROVAL-001)
  getPendingApprovals: async () => unwrap(await apiClient.get('/approvals/pending')),
  submitApprovalDecision: async (proposalId, decisionData) => unwrap(await apiClient.post(`/approvals/${proposalId}/decision`, decisionData)),

  // Service Alerts
  getAlerts: async () => unwrap(await apiClient.get('/alerts')),
  broadcastAlert: async (alertData) => unwrap(await apiClient.post('/alerts', alertData)),

  // AI Workflow Observability & Traces
  getAiWorkflows: async (params) => unwrap(await apiClient.get('/ai/workflows', { params })),
  getAiWorkflowById: async (workflowId) => unwrap(await apiClient.get(`/ai/workflows/${workflowId}`)),

  // Helpers
  getServices: async () => unwrap(await apiClient.get('/dev/services')),
  getAuditLogs: async (params) => unwrap(await apiClient.get('/dev/audit-logs', { params })),
}
