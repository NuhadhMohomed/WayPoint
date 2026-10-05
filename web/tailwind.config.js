/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ["class"],
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Authoritative Brand Color Tokens Synchronized with Flutter Mobile AppTheme
        waypoint: {
          // Primary Brand - Spring Green & Forest Black (WCAG AAA >= 7:1)
          primary: "#32DE84",
          onPrimary: "#042611",
          primaryDark: "#1EAE60",
          primaryContainerLight: "#D9FBE8",
          primaryContainerDark: "#0E3820",

          // Semantic Accents
          amber: "#F59E0B", // Sunset Amber (10m seat hold & buffer warning)
          darkAmber: "#B45309",
          sky: "#0284C7", // Sky Blue (Corridors & expressways E01, E04)
          darkSky: "#0369A1",
          error: "#EF4444", // Crimson Alert
          darkError: "#B91C1C",

          // Dark Operations Cockpit Surfaces (Default Theme)
          darkBg: "#090D16", // Midnight Navy / Main canvas
          darkSurface: "#131B2E", // Deep Navy Slate / Card & sidebar surface
          darkSubdued: "#1A243B", // Elevated Navy Slate / Header & subdued panels
          darkBorder: "#23304D", // Crisp Navy Slate border
          darkText: "#F8FAFC", // Primary text in dark mode
          darkMuted: "#94A3B8", // Muted text in dark mode

          // High-Visibility Light Surfaces
          lightBg: "#F8FAFC", // Cool off-white canvas
          lightSurface: "#FFFFFF", // Pure white card
          lightSubdued: "#F1F5F9", // Slate-100 headers & inputs
          lightBorder: "#E2E8F0", // Slate-200 border
          lightText: "#0F172A", // Primary text in light mode
          lightMuted: "#64748B", // Muted text in light mode

          // Backward-compatibility aliases for legacy templates
          blue: "#0284C7",
          darkBlue: "#0369A1",
          green: "#32DE84",
          lightGreen: "#D9FBE8",
          surface: "#131B2E",
          card: "#131B2E",
          text: "#F8FAFC",
          muted: "#94A3B8",
          border: "#23304D",
          alert: "#EF4444",
        },

        // CSS Variables for dynamic theme switching
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "#32DE84",
          foreground: "#042611",
          dark: "#1EAE60",
        },
        secondary: {
          DEFAULT: "#F59E0B",
          foreground: "#090D16",
        },
        tertiary: {
          DEFAULT: "#0284C7",
          foreground: "#FFFFFF",
        },
        destructive: {
          DEFAULT: "#EF4444",
          foreground: "#FFFFFF",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "#32DE84",
          foreground: "#042611",
        },
        popover: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
      },
      fontFamily: {
        display: ['"Plus Jakarta Sans"', 'system-ui', 'sans-serif'],
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'monospace'],
      },
      spacing: {
        'waypoint-xs': '4px',
        'waypoint-sm': '12px',
        'waypoint-md': '16px',
        'waypoint-lg': '24px',
        'waypoint-xl': '32px',
      },
      borderRadius: {
        xl: "1rem",
        lg: "0.75rem",
        md: "0.5rem",
        sm: "0.25rem",
      },
      boxShadow: {
        'glow-primary': '0 0 20px -3px rgba(50, 222, 132, 0.25)',
        'glow-amber': '0 0 20px -3px rgba(245, 158, 11, 0.25)',
        'card-dark': '0 4px 20px -2px rgba(0, 0, 0, 0.35)',
        'card-light': '0 4px 20px -2px rgba(15, 23, 42, 0.05)',
      },
    },
  },
  plugins: [],
}
