import { apiClient } from '@/api/client'

export const fleetApi = {
  // ─── Bus Endpoints ───
  getBuses: async (params) => {
    const res = await apiClient.get('/buses', { params })
    return res.data
  },
  getBusById: async (busId) => {
    const res = await apiClient.get(`/buses/${busId}`)
    return res.data
  },
  createBus: async (busData) => {
    const res = await apiClient.post('/buses', busData)
    return res.data
  },
  updateBusMaintenance: async (busId, maintenanceData) => {
    const res = await apiClient.put(`/buses/${busId}/maintenance`, maintenanceData)
    return res.data
  },

  // ─── Seat Layout Endpoints ───
  getSeatLayouts: async () => {
    const res = await apiClient.get('/seats/layouts')
    return res.data
  },
  getSeatLayoutById: async (layoutId) => {
    const res = await apiClient.get(`/seats/layouts/${layoutId}`)
    return res.data
  },
  createSeatLayout: async (layoutData) => {
    const res = await apiClient.post('/seats/layouts', layoutData)
    return res.data
  },

  // ─── Driver Endpoints ───
  getDrivers: async (params) => {
    const res = await apiClient.get('/drivers', { params })
    return res.data
  },
  getDriverById: async (driverId) => {
    const res = await apiClient.get(`/drivers/${driverId}`)
    return res.data
  },
  createDriver: async (driverData) => {
    const res = await apiClient.post('/drivers', driverData)
    return res.data
  },
  assignDriver: async (assignmentData) => {
    const res = await apiClient.post('/drivers/assign', assignmentData)
    return res.data
  },
  getDriverAssignments: async (driverId) => {
    const res = await apiClient.get(`/drivers/${driverId}/assignments`)
    return res.data
  },

  // ─── Seat Availability ───
  getServiceSeatMatrix: async (serviceId) => {
    const res = await apiClient.get(`/services/${serviceId}/seats`)
    return res.data
  },

  // ─── Resource Feasibility ───
  evaluateResourceFeasibility: async (feasibilityData) => {
    const res = await apiClient.post('/resources/replacement-feasibility', feasibilityData)
    return res.data
  },

  // ─── Review Endpoints ───
  submitBusReview: async (reviewData) => {
    const res = await apiClient.post('/reviews/buses', reviewData)
    return res.data
  },
  submitDriverReview: async (reviewData) => {
    const res = await apiClient.post('/reviews/drivers', reviewData)
    return res.data
  },
  updateBusReview: async (reviewId, reviewData) => {
    const res = await apiClient.put(`/reviews/buses/${reviewId}`, reviewData)
    return res.data
  },
  updateDriverReview: async (reviewId, reviewData) => {
    const res = await apiClient.put(`/reviews/drivers/${reviewId}`, reviewData)
    return res.data
  },
  deleteBusReview: async (reviewId) => {
    const res = await apiClient.delete(`/reviews/buses/${reviewId}`)
    return res.data
  },
  deleteDriverReview: async (reviewId) => {
    const res = await apiClient.delete(`/reviews/drivers/${reviewId}`)
    return res.data
  },
  getBusReviews: async (busId, params) => {
    const res = await apiClient.get(`/reviews/buses/${busId}`, { params })
    return res.data
  },
  getDriverReviews: async (driverId, params) => {
    const res = await apiClient.get(`/reviews/drivers/${driverId}`, { params })
    return res.data
  },
  getBusRatingSummary: async (busId) => {
    const res = await apiClient.get(`/reviews/buses/${busId}/summary`)
    return res.data
  },
  getDriverRatingSummary: async (driverId) => {
    const res = await apiClient.get(`/reviews/drivers/${driverId}/summary`)
    return res.data
  },
}
