/**
 * Token store untuk token yang berasal dari DWI Mobile bridge.
 *
 * Access token  → memory only (hilang saat refresh halaman — by design)
 * Refresh token → sessionStorage (bertahan dalam tab yang sama, hilang saat tab ditutup)
 *
 * Mengapa BUKAN localStorage:
 *   - localStorage dapat dibaca script manapun di domain sama (XSS risk)
 *   - sessionStorage scope-nya per-tab, tidak bocor ke tab lain
 *   - Memory hilang saat refresh → memaksa re-auth via DWI Mobile atau refresh token
 *
 * Refresh token TIDAK disimpan di memory-only karena kita ingin page refresh tetap bisa kerja
 * dalam satu sesi tab. Saat tab ditutup, session berakhir secara otomatis.
 */

const REFRESH_KEY = '_lms_bridge_rt'   // sessionStorage key — nama tidak obvious untuk anti-XSS recon

// ── In-memory state ───────────────────────────────────────────────────────────

let _accessToken  = null
let _accessExpiry = 0     // epoch ms — kapan access token expires (minus 30s buffer)

// ── Write ─────────────────────────────────────────────────────────────────────

/**
 * Simpan token baru ke store.
 * @param {string} accessToken
 * @param {string|null} refreshToken  null jika tidak ada (token exchange tertentu tidak return refresh)
 * @param {number} expiresIn          detik sampai access token expires (dari Keycloak response)
 */
export function setTokens(accessToken, refreshToken, expiresIn) {
  _accessToken  = accessToken
  _accessExpiry = Date.now() + Math.max(0, expiresIn - 30) * 1000  // 30s safety buffer

  if (refreshToken) {
    sessionStorage.setItem(REFRESH_KEY, refreshToken)
  }
}

/**
 * Hapus semua token (logout atau error).
 */
export function clearTokens() {
  _accessToken  = null
  _accessExpiry = 0
  sessionStorage.removeItem(REFRESH_KEY)
}

// ── Read ──────────────────────────────────────────────────────────────────────

export function getAccessToken()  { return isAccessTokenValid() ? _accessToken : null }
export function getRefreshToken() { return sessionStorage.getItem(REFRESH_KEY) }

export function isAccessTokenValid() {
  return _accessToken !== null && Date.now() < _accessExpiry
}

/**
 * Apakah ada bridge session yang bisa dilanjutkan?
 * True jika:
 *   - Ada access token yang masih valid (normal case), ATAU
 *   - Ada refresh token di sessionStorage (page refresh case — bisa di-refresh)
 */
export function hasBridgeSession() {
  return isAccessTokenValid() || !!getRefreshToken()
}

// ── Refresh ───────────────────────────────────────────────────────────────────

/**
 * Refresh access token via PHP DWI Mobile endpoint.
 * PHP menyimpan client_secret — frontend tidak perlu tahu secret.
 *
 * @returns {boolean} true jika berhasil
 */
export async function refreshAccessToken() {
  const refreshToken = getRefreshToken()
  if (!refreshToken) return false

  const dwiMobileUrl = import.meta.env.VITE_DWI_MOBILE_URL
  if (!dwiMobileUrl) {
    console.error('[tokenStore] VITE_DWI_MOBILE_URL tidak dikonfigurasi')
    return false
  }

  try {
    const res = await fetch(`${dwiMobileUrl}/api/auth/lms-refresh`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify({ refresh_token: refreshToken }),
    })

    if (!res.ok) {
      clearTokens()
      return false
    }

    const { access_token, refresh_token, expires_in } = await res.json()
    setTokens(access_token, refresh_token ?? refreshToken, expires_in)
    return true
  } catch {
    clearTokens()
    return false
  }
}

// ── One-time code exchange ────────────────────────────────────────────────────

/**
 * Tukar satu kali code dari URL ?_lms_auth= dengan token sesungguhnya.
 * Code disimpan di server PHP (bukan di URL), sehingga aman dicuri dari URL.
 *
 * @param {string} code  UUID dari PHP
 * @returns {boolean}    true jika berhasil
 */
export async function exchangeAuthCode(code) {
  const dwiMobileUrl = import.meta.env.VITE_DWI_MOBILE_URL
  if (!dwiMobileUrl) return false

  try {
    const res = await fetch(`${dwiMobileUrl}/api/auth/lms-exchange`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify({ code }),
    })

    if (!res.ok) return false

    const { access_token, refresh_token, expires_in } = await res.json()
    setTokens(access_token, refresh_token, expires_in)
    return true
  } catch {
    return false
  }
}
