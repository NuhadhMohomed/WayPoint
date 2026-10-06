import axios from 'axios';
import { useAuthStore } from '../store/authStore';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5010/api/v1',
  headers: {
    'Content-Type': 'application/json'
  },
  timeout: 15000
});

// Request Interceptor: Attach JWT Bearer token
apiClient.interceptors.request.use(
  (config) => {
    const token = useAuthStore.getState().token || localStorage.getItem('waypoint_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Format errors & handle 401/403
apiClient.interceptors.response.use(
  (response) => response.data,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }

    const message =
      error.response?.data?.detail ||
      error.response?.data?.message ||
      error.message ||
      'An unexpected error occurred while communicating with the WayPoint server.';

    return Promise.reject(new Error(message));
  }
);
