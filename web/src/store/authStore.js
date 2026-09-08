import { create } from 'zustand'

export const useAuthStore = create((set) => ({
  token: localStorage.getItem('waypoint_token') || null,
  user: JSON.parse(localStorage.getItem('waypoint_user') || 'null'),
  isAuthenticated: !!localStorage.getItem('waypoint_token'),

  setAuth: (token, user) => {
    localStorage.setItem('waypoint_token', token)
    localStorage.setItem('waypoint_user', JSON.stringify(user))
    set({ token, user, isAuthenticated: true })
  },

  logout: () => {
    localStorage.removeItem('waypoint_token')
    localStorage.removeItem('waypoint_user')
    set({ token: null, user: null, isAuthenticated: false })
  },
}))
