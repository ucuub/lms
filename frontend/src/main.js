import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './style.css'
import App from './App.vue'
import AccessDenied from './views/AccessDenied.vue'
import router from './router'
import keycloak from './auth/keycloak'
import { useAuthStore } from './stores/auth'
import api from './api/axios'
import {
  hasBridgeSession,
  isAccessTokenValid,
  refreshAccessToken,
  exchangeAuthCode,
  clearTokens,
} from './auth/tokenStore'

// ── Bootstrap helpers ─────────────────────────────────────────────────────────

async function syncAndMountApp(pinia) {
  const app = createApp(App)
  app.use(pinia)
  app.use(router)

  const auth = useAuthStore()
  try {
    const { data } = await api.post('/auth/sync')
    auth.setUser(data)
  } catch (e) {
    console.error('[main] Auth sync failed:', e)
  }

  app.mount('#app')
}

function mountAccessDenied(pinia) {
  const app = createApp(AccessDenied)
  app.use(pinia)
  app.mount('#app')
}

// ── Entry point ───────────────────────────────────────────────────────────────

;(async () => {
  const pinia = createPinia()

  // ── LOCAL DEV MODE: bypass Keycloak sepenuhnya ────────────────────────────
  // Set VITE_MOCK_AUTH=true di frontend/.env.local untuk dev tanpa Keycloak.
  // Backend harus pakai MockUserContext (X-User-Id/X-User-Role headers).
  if (import.meta.env.VITE_MOCK_AUTH === 'true') {
    await syncAndMountApp(pinia)
    return
  }

  // ── Cek apakah ada one-time auth code dari DWI Mobile ────────────────────
  const urlParams  = new URLSearchParams(window.location.search)
  const lmsCode    = urlParams.get('_lms_auth')

  if (lmsCode) {
    // ── BRIDGE FLOW A: Redirect dari DWI Mobile dengan one-time code ────────
    // Hapus code dari URL segera — sebelum apapun, agar tidak tersimpan di history
    window.history.replaceState({}, '', window.location.pathname)

    const ok = await exchangeAuthCode(lmsCode)
    if (!ok) {
      mountAccessDenied(pinia)
      return
    }

    await syncAndMountApp(pinia)
    return
  }

  if (hasBridgeSession()) {
    // ── BRIDGE FLOW B: Page refresh dalam tab yang sama ─────────────────────
    // sessionStorage punya refresh token → minta access token baru via PHP
    if (!isAccessTokenValid()) {
      const refreshed = await refreshAccessToken()
      if (!refreshed) {
        clearTokens()
        mountAccessDenied(pinia)
        return
      }
    }

    await syncAndMountApp(pinia)
    return
  }

  // ── KEYCLOAK SSO FLOW: Cek apakah ada session Keycloak di browser ─────────
  // DEMO_MODE=true  → 'login-required' (redirect ke Keycloak login jika belum auth)
  // DEMO_MODE=false → 'check-sso' (hanya terima jika sudah ada session, tidak redirect)
  const onLoad = import.meta.env.VITE_DEMO_MODE === 'true' ? 'login-required' : 'check-sso'

  keycloak
    .init({
      onLoad,
      checkLoginIframe: false,
      ...(onLoad === 'check-sso' && {
        silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
      }),
    })
    .then(async (authenticated) => {
      if (!authenticated) {
        mountAccessDenied(pinia)
        return
      }
      await syncAndMountApp(pinia)
    })
    .catch((err) => {
      console.error('[main] Keycloak init failed:', err)
      mountAccessDenied(pinia)
    })
})()
