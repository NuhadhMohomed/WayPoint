/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}"
  ],
  theme: {
    extend: {
      colors: {
        waypoint: {
          primary: '#4F46E5',
          'primary-hover': '#4338CA',
          onPrimary: '#FFFFFF',
          blue: '#0056D2',
          'blue-dark': '#003E9A',
          'blue-light': '#EBF2FF',
          amber: '#FEB300',
          'amber-dark': '#C68A00',
          'amber-light': '#FFF8E6',
          green: '#005312',
          'green-dark': '#003D0D',
          'green-light': '#E6F4EA',
          slate: '#0F172A',
          canvas: '#F8FAFC',
          card: '#FFFFFF',
          border: '#E2E8F0',
          'dark-canvas': '#0B132B',
          'dark-card': '#1C2541',
          'dark-border': '#3A506B'
        }
      },
      fontFamily: {
        heading: ['"Plus Jakarta Sans"', 'sans-serif'],
        sans: ['Inter', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'monospace']
      },
      borderRadius: {
        'transit': '0.75rem'
      }
    }
  },
  plugins: []
};
