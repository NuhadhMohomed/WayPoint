import { create } from 'zustand'

export const useDisruptionStore = create((set) => ({
  // Disruption cases state
  disruptions: [],
  disruptionsLoading: false,
  disruptionPagination: { pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
  activeDisruption: null,
  activeDisruptionImpact: null,
  impactLoading: false,

  // Pending manager approvals state
  pendingApprovals: [],
  approvalsLoading: false,

  // Public service alerts state
  serviceAlerts: [],
  alertsLoading: false,

  // AI Workflow Observability state
  aiWorkflows: [],
  workflowsLoading: false,
  workflowPagination: { pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
  activeWorkflow: null,
  activeWorkflowLoading: false,

  // Audit Logs state
  auditLogs: [],
  auditLogsLoading: false,

  // Transit services for dropdowns
  services: [],
  servicesLoading: false,

  // Actions
  setDisruptions: (data) => set({
    disruptions: data.items || data,
    disruptionPagination: {
      pageNumber: data.pageNumber || 1,
      pageSize: data.pageSize || 20,
      totalCount: data.totalCount || (data.items ? data.items.length : data.length || 0),
      totalPages: data.totalPages || 1,
    },
  }),
  setDisruptionsLoading: (loading) => set({ disruptionsLoading: loading }),

  setActiveDisruption: (disruption) => set({ activeDisruption: disruption }),
  setActiveDisruptionImpact: (impact) => set({ activeDisruptionImpact: impact }),
  setImpactLoading: (loading) => set({ impactLoading: loading }),

  setPendingApprovals: (approvals) => set({ pendingApprovals: approvals }),
  setApprovalsLoading: (loading) => set({ approvalsLoading: loading }),

  setServiceAlerts: (alerts) => set({ serviceAlerts: alerts }),
  setAlertsLoading: (loading) => set({ alertsLoading: loading }),

  setAiWorkflows: (data) => set({
    aiWorkflows: data.items || data,
    workflowPagination: {
      pageNumber: data.pageNumber || 1,
      pageSize: data.pageSize || 20,
      totalCount: data.totalCount || (data.items ? data.items.length : data.length || 0),
      totalPages: data.totalPages || 1,
    },
  }),
  setWorkflowsLoading: (loading) => set({ workflowsLoading: loading }),

  setActiveWorkflow: (workflow) => set({ activeWorkflow: workflow }),
  setActiveWorkflowLoading: (loading) => set({ activeWorkflowLoading: loading }),

  setAuditLogs: (logs) => set({ auditLogs: logs }),
  setAuditLogsLoading: (loading) => set({ auditLogsLoading: loading }),

  setServices: (services) => set({ services: services }),
  setServicesLoading: (loading) => set({ servicesLoading: loading }),
}))
