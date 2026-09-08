import { apiClient } from '@/api/client'

export const fleetApi = {
  getBuses: async (params) => {
    const res = await apiClient.get('/buses', { params })
    return res.data
  },
  createBus: async (busData) => {
    const res = await apiClient.post('/buses', busData)
    return res.data
  },
  updateBusMaintenance: async (busId, isUnderMaintenance) => {
    const res = await apiClient.put(`/buses/${busId}/maintenance`, { isUnderMaintenance })
    return res.data
  },
  getSeatLayouts: async () => {
    const res = await apiClient.get('/seats/layouts')
    return res.data
  },
  createSeatLayout: async (layoutData) => {
    const res = await apiClient.post('/seats/layouts', layoutData)
    return res.data
  },
  getDrivers: async (params) => {
    const res = await apiClient.get('/drivers', { params })
    return res.data
  },
  getServiceSeatMatrix: async (serviceId) => {
    const res = await apiClient.get(`/services/${serviceId}/seats`)
    return res.data
  },
}
