import { create } from 'zustand'

const getInitialTheme = () => {
  if (typeof window === 'undefined') return 'dark'
  const saved = localStorage.getItem('waypoint_theme')
  if (saved === 'light' || saved === 'dark') return saved
  return 'dark'
}

const getInitialDensity = () => {
  if (typeof window === 'undefined') return 'spacious'
  const saved = localStorage.getItem('waypoint_density')
  if (saved === 'compact' || saved === 'spacious') return saved
  return 'spacious'
}

const applyThemeToDom = (theme) => {
  if (typeof document === 'undefined') return
  if (theme === 'dark') {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }
}

const applyDensityToDom = (density) => {
  if (typeof document === 'undefined') return
  if (density === 'compact') {
    document.documentElement.classList.add('density-compact')
  } else {
    document.documentElement.classList.remove('density-compact')
  }
}

const initialTheme = getInitialTheme()
const initialDensity = getInitialDensity()
applyThemeToDom(initialTheme)
applyDensityToDom(initialDensity)

export const useThemeStore = create((set, get) => ({
  theme: initialTheme,
  density: initialDensity,

  setTheme: (theme) => {
    localStorage.setItem('waypoint_theme', theme)
    applyThemeToDom(theme)
    set({ theme })
  },

  toggleTheme: () => {
    const next = get().theme === 'dark' ? 'light' : 'dark'
    localStorage.setItem('waypoint_theme', next)
    applyThemeToDom(next)
    set({ theme: next })
  },

  setDensity: (density) => {
    localStorage.setItem('waypoint_density', density)
    applyDensityToDom(density)
    set({ density })
  },

  toggleDensity: () => {
    const next = get().density === 'spacious' ? 'compact' : 'spacious'
    localStorage.setItem('waypoint_density', next)
    applyDensityToDom(next)
    set({ density: next })
  },
}))
