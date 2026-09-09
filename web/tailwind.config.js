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
        // Stitch DESIGN.md Brand Tokens
        waypoint: {
          blue: "#0056D2",
          darkBlue: "#0040A1",
          amber: "#FEB300",
          darkAmber: "#7E5700",
          green: "#005312",
          lightGreen: "#1C6D24",
          surface: "#F8F9FA",
          card: "#FFFFFF",
          text: "#191C1D",
          muted: "#424654",
          border: "#C3C6D6",
          alert: "#BA1A1A"
        },
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "#0056D2",
          foreground: "#FFFFFF",
        },
        secondary: {
          DEFAULT: "#FEB300",
          foreground: "#191C1D",
        },
        destructive: {
          DEFAULT: "#BA1A1A",
          foreground: "#FFFFFF",
        },
        muted: {
          DEFAULT: "#424654",
          foreground: "#FFFFFF",
        },
        accent: {
          DEFAULT: "#FEB300",
          foreground: "#191C1D",
        },
        popover: {
          DEFAULT: "#FFFFFF",
          foreground: "#191C1D",
        },
        card: {
          DEFAULT: "#FFFFFF",
          foreground: "#191C1D",
        },
      },
      fontFamily: {
        display: ['"Plus Jakarta Sans"', 'system-ui', 'sans-serif'],
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
    },
  },
  plugins: [],
}
