import { create } from 'zustand';

export const useAuthStore = create((set) => {
  // Restore from localStorage if present
  const storedToken = localStorage.getItem('waypoint_token');
  const storedUser = localStorage.getItem('waypoint_user')
    ? JSON.parse(localStorage.getItem('waypoint_user'))
    : null;

  return {
    token: storedToken,
    user: storedUser,
    isAuthenticated: Boolean(storedToken),
    isAdmin: storedUser?.role === 'Admin',
    isManager: storedUser?.role === 'TransportManager',
    isOperator: storedUser?.role === 'Operator',
    isPassenger: storedUser?.role === 'Passenger',

    setAuth: (token, user) => {
      localStorage.setItem('waypoint_token', token);
      localStorage.setItem('waypoint_user', JSON.stringify(user));
      set({
        token,
        user,
        isAuthenticated: true,
        isAdmin: user?.role === 'Admin',
        isManager: user?.role === 'TransportManager',
        isOperator: user?.role === 'Operator',
        isPassenger: user?.role === 'Passenger'
      });
    },

    logout: () => {
      localStorage.removeItem('waypoint_token');
      localStorage.removeItem('waypoint_user');
      set({
        token: null,
        user: null,
        isAuthenticated: false,
        isAdmin: false,
        isManager: false,
        isOperator: false,
        isPassenger: false
      });
    },

    updateUser: (updatedFields) => {
      set((state) => {
        const newUser = { ...state.user, ...updatedFields };
        localStorage.setItem('waypoint_user', JSON.stringify(newUser));
        return {
          user: newUser,
          isAdmin: newUser?.role === 'Admin',
          isManager: newUser?.role === 'TransportManager',
          isOperator: newUser?.role === 'Operator',
          isPassenger: newUser?.role === 'Passenger'
        };
      });
    }
  };
});
