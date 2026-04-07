import axios from 'axios'
import keycloak from '@/auth/keycloak'
import {
  hasBridgeSession,
  isAccessTokenValid,
  getAccessToken,
  refreshAccessToken,
  clearTokens,
} from '@/auth/tokenStore'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  timeout: 15000,
})

// ── Request interceptor ───────────────────────────────────────────────────────

api.interceptors.request.use(async (config) => {
  // ── Path A: Token dari DWI Mobile bridge ─────────────────────────────────
  if (hasBridgeSession()) {
    // Access token expired → refresh dulu via PHP endpoint
    if (!isAccessTokenValid()) {
      const refreshed = await refreshAccessToken()
      if (!refreshed) {
        clearTokens()
        // Reload → hasBridgeSession() = false → AccessDenied akan muncul
        window.location.reload()
        return Promise.reject(new Error('Token expired dan gagal di-refresh'))
      }
    }

    config.headers.Authorization = `Bearer ${getAccessToken()}`
    return config
  }

  // ── Path B: Keycloak JS adapter (check-sso session) ──────────────────────
  if (keycloak.authenticated) {
    try {
      await keycloak.updateToken(30)
    } catch {
      // Keycloak session expired — tampil AccessDenied, bukan redirect ke login
      window.location.reload()
      return Promise.reject(new Error('Keycloak session expired'))
    }
    config.headers.Authorization = `Bearer ${keycloak.token}`
    return config
  }

  // Tidak ada session sama sekali
  return Promise.reject(new Error('Not authenticated'))
})

export default api
