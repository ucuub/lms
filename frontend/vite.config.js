import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 5173,
    host: true,
    allowedHosts: 'all',
    proxy: {
      '/api': { target: 'http://localhost:5000', changeOrigin: true, proxyTimeout: 300000, timeout: 300000 },
      '/uploads': { target: 'http://localhost:5000', changeOrigin: true }
    }
  }
})
