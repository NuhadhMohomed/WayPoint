import { create } from 'zustand';

export const useThemeStore = create((set) => {
  const storedTheme = localStorage.getItem('waypoint_theme') || 'light';
  if (storedTheme === 'dark') {
    document.documentElement.classList.add('dark');
  }

  return {
    isDarkMode: storedTheme === 'dark',
    sidebarCollapsed: localStorage.getItem('waypoint_sidebar') === 'collapsed',
    soundEnabled: localStorage.getItem('waypoint_sound') !== 'disabled',

    toggleDarkMode: () => {
      set((state) => {
        const next = !state.isDarkMode;
        if (next) {
          document.documentElement.classList.add('dark');
          localStorage.setItem('waypoint_theme', 'dark');
        } else {
          document.documentElement.classList.remove('dark');
          localStorage.setItem('waypoint_theme', 'light');
        }
        return { isDarkMode: next };
      });
    },

    toggleSidebar: () => {
      set((state) => {
        const next = !state.sidebarCollapsed;
        localStorage.setItem('waypoint_sidebar', next ? 'collapsed' : 'expanded');
        return { sidebarCollapsed: next };
      });
    },

    toggleSound: () => {
      set((state) => {
        const next = !state.soundEnabled;
        localStorage.setItem('waypoint_sound', next ? 'enabled' : 'disabled');
        return { soundEnabled: next };
      });
    }
  };
});
