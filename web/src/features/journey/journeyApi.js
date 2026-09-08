import { apiClient } from '@/api/client'

export const journeyApi = {
  getRoutes: async (params) => {
    const res = await apiClient.get('/routes', { params })
    return res.data
  },
  getRouteById: async (id) => {
    const res = await apiClient.get(`/routes/${id}`)
    return res.data
  },
  createRoute: async (routeData) => {
    const res = await apiClient.post('/routes', routeData)
    return res.data
  },
  getServices: async (params) => {
    const res = await apiClient.get('/services', { params })
    return res.data
  },
  getServiceById: async (id) => {
    const res = await apiClient.get(`/services/${id}`)
    return res.data
  },
  searchJourneys: async (searchParams) => {
    const res = await apiClient.post('/journeys/search', searchParams)
    return res.data
  },
}
