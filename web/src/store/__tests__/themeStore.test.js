import { describe, it, expect, beforeEach } from 'vitest'
import { useThemeStore } from '../themeStore'

describe('ThemeStore', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.className = ''
    useThemeStore.setState({ theme: 'dark', density: 'spacious' })
  })

  it('initializes with dark theme and spacious density by default', () => {
    expect(useThemeStore.getState().theme).toBe('dark')
    expect(useThemeStore.getState().density).toBe('spacious')
  })

  it('toggles between dark and light theme and manages .dark class on html element', () => {
    useThemeStore.getState().toggleTheme()
    expect(useThemeStore.getState().theme).toBe('light')
    expect(document.documentElement.classList.contains('dark')).toBe(false)
    expect(localStorage.getItem('waypoint_theme')).toBe('light')

    useThemeStore.getState().toggleTheme()
    expect(useThemeStore.getState().theme).toBe('dark')
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(localStorage.getItem('waypoint_theme')).toBe('dark')
  })

  it('toggles density between spacious and compact and manages .density-compact class on html element', () => {
    useThemeStore.getState().toggleDensity()
    expect(useThemeStore.getState().density).toBe('compact')
    expect(document.documentElement.classList.contains('density-compact')).toBe(true)
    expect(localStorage.getItem('waypoint_density')).toBe('compact')

    useThemeStore.getState().toggleDensity()
    expect(useThemeStore.getState().density).toBe('spacious')
    expect(document.documentElement.classList.contains('density-compact')).toBe(false)
    expect(localStorage.getItem('waypoint_density')).toBe('spacious')
  })

  it('allows explicit setTheme and setDensity', () => {
    useThemeStore.getState().setTheme('light')
    expect(useThemeStore.getState().theme).toBe('light')
    expect(document.documentElement.classList.contains('dark')).toBe(false)

    useThemeStore.getState().setDensity('compact')
    expect(useThemeStore.getState().density).toBe('compact')
    expect(document.documentElement.classList.contains('density-compact')).toBe(true)
  })
})
