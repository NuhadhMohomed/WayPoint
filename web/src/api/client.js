import axios from 'axios'

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api/v1'

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Attach JWT token to all requests
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('waypoint_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Handle 401 Unauthorized globally
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('waypoint_token')
      localStorage.removeItem('waypoint_user')
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)

export const authApi = {
  login: async (credentials) => {
    const res = await apiClient.post('/auth/login', credentials)
    return res.data
  },
  register: async (userData) => {
    const res = await apiClient.post('/auth/register', userData)
    return res.data
  },
  getCurrentUser: async () => {
    const res = await apiClient.get('/auth/me')
    return res.data
  },
}

export const devApi = {
  seedDatabase: async () => {
    const res = await apiClient.post('/dev/seed')
    return res.data
  },
  getStatus: async () => {
    const res = await apiClient.get('/dev/status')
    return res.data
  },
}
