import { apiClient } from '../../api/client'

export const journeyApi = {
  getRoutes: async (params) => {
    const res = await apiClient.get('/routes', { params })
    return res?.data ?? res
  },
  getRouteById: async (id) => {
    const res = await apiClient.get(`/routes/${id}`)
    return res?.data ?? res
  },
  createRoute: async (routeData) => {
    const res = await apiClient.post('/routes', routeData)
    return res?.data ?? res
  },
  getServices: async (params) => {
    const res = await apiClient.get('/services', { params })
    return res?.data ?? res
  },
  getServiceById: async (id) => {
    const res = await apiClient.get(`/services/${id}`)
    return res?.data ?? res
  },
  searchJourneys: async (searchParams) => {
    const res = await apiClient.post('/journeys/search', searchParams)
    return res?.data ?? res
  },
  getAiRecommendations: async (objective) => {
    const res = await apiClient.post('/ai/journey-recommendation', { objective })
    return res?.data ?? res
  },
}
