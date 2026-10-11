import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// In development, forward API calls to the .NET backend.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
})
