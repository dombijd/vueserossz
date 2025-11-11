/** @type {import('tailwindcss').Config} */
export default {
    content: [
      "./index.html",
      "./src/**/*.{vue,js,ts,jsx,tsx}",
    ],
    theme: {
      extend: {
        colors: {
          primary: {
            DEFAULT: '#2563EB',
            dark: '#1E40AF',
            light: '#DBEAFE',
          },
          success: '#10B981',
          warning: '#F59E0B',
          danger: '#EF4444',
          info: '#6366F1',
        },
        spacing: {
          'xs': '4px',
          'sm': '8px',
          'md': '16px',
          'lg': '24px',
          'xl': '32px',
          '2xl': '48px',
          '3xl': '64px',
        },
        borderRadius: {
          'sm': '4px',
          'md': '8px',
          'lg': '12px',
          'xl': '16px',
        }
      },
    },
    plugins: [],
  }