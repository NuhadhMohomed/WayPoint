import { create } from 'zustand';

// Self-healing: Enforce authoritative light mode and remove any stale dark classes/tokens
if (typeof document !== 'undefined') {
  document.documentElement.classList.remove('dark');
}
if (typeof localStorage !== 'undefined') {
  localStorage.removeItem('waypoint_theme');
}

export const useThemeStore = create((set) => {
  return {
    isDarkMode: false,
    sidebarCollapsed: typeof localStorage !== 'undefined' ? localStorage.getItem('waypoint_sidebar') === 'collapsed' : false,
    soundEnabled: typeof localStorage !== 'undefined' ? localStorage.getItem('waypoint_sound') !== 'disabled' : true,

    // Safe no-op preserved for backwards compatibility with any legacy callers
    toggleDarkMode: () => {
      if (typeof document !== 'undefined') {
        document.documentElement.classList.remove('dark');
      }
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
