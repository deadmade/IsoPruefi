import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/v1': {
        target: 'https://localhost:5160',
        changeOrigin: true,   
        secure: false,        
      },
    },
  },
});
