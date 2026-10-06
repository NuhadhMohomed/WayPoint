import { apiClient } from '@/api/client'

const unwrap = (res) => (res && res.data !== undefined && !res.items && !Array.isArray(res) ? res.data : res)

export const fleetApi = {
  // ─── Bus Endpoints ───
  getBuses: async (params) => unwrap(await apiClient.get('/buses', { params })),
  getBusById: async (busId) => unwrap(await apiClient.get(`/buses/${busId}`)),
  createBus: async (busData) => unwrap(await apiClient.post('/buses', busData)),
  updateBusMaintenance: async (busId, maintenanceData) => unwrap(await apiClient.put(`/buses/${busId}/maintenance`, maintenanceData)),

  // ─── Seat Layout Endpoints ───
  getSeatLayouts: async () => unwrap(await apiClient.get('/seats/layouts')),
  getSeatLayoutById: async (layoutId) => unwrap(await apiClient.get(`/seats/layouts/${layoutId}`)),
  createSeatLayout: async (layoutData) => unwrap(await apiClient.post('/seats/layouts', layoutData)),

  // ─── Driver Endpoints ───
  getDrivers: async (params) => unwrap(await apiClient.get('/drivers', { params })),
  getDriverById: async (driverId) => unwrap(await apiClient.get(`/drivers/${driverId}`)),
  createDriver: async (driverData) => unwrap(await apiClient.post('/drivers', driverData)),
  assignDriver: async (assignmentData) => unwrap(await apiClient.post('/drivers/assign', assignmentData)),
  getDriverAssignments: async (driverId) => unwrap(await apiClient.get(`/drivers/${driverId}/assignments`)),

  // ─── Services / Corridors ───
  getServices: async () => unwrap(await apiClient.get('/dev/services')),

  // ─── Seat Availability ───
  getServiceSeatMatrix: async (serviceId) => unwrap(await apiClient.get(`/services/${serviceId}/seats`)),

  // ─── Resource Feasibility ───
  evaluateResourceFeasibility: async (feasibilityData) => unwrap(await apiClient.post('/resources/replacement-feasibility', feasibilityData)),

  // ─── Review Endpoints ───
  submitBusReview: async (reviewData) => unwrap(await apiClient.post('/reviews/buses', reviewData)),
  submitDriverReview: async (reviewData) => unwrap(await apiClient.post('/reviews/drivers', reviewData)),
  updateBusReview: async (reviewId, reviewData) => unwrap(await apiClient.put(`/reviews/buses/${reviewId}`, reviewData)),
  updateDriverReview: async (reviewId, reviewData) => unwrap(await apiClient.put(`/reviews/drivers/${reviewId}`, reviewData)),
  deleteBusReview: async (reviewId) => unwrap(await apiClient.delete(`/reviews/buses/${reviewId}`)),
  deleteDriverReview: async (reviewId) => unwrap(await apiClient.delete(`/reviews/drivers/${reviewId}`)),
  getBusReviews: async (busId, params) => unwrap(await apiClient.get(`/reviews/buses/${busId}`, { params })),
  getDriverReviews: async (driverId, params) => unwrap(await apiClient.get(`/reviews/drivers/${driverId}`, { params })),
  getBusRatingSummary: async (busId) => unwrap(await apiClient.get(`/reviews/buses/${busId}/summary`)),
  getDriverRatingSummary: async (driverId) => unwrap(await apiClient.get(`/reviews/drivers/${driverId}/summary`)),
}
