import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import keycloak from '@/auth/keycloak'
import { hasBridgeSession, clearTokens } from '@/auth/tokenStore'
import api from '@/api/axios'

export const useAuthStore = defineStore('auth', () => {
  const user = ref(null)

  // Authenticated jika ada bridge session ATAU keycloak adapter authenticated
  const isAuthenticated = computed(() => (hasBridgeSession() || keycloak.authenticated) && !!user.value)
  const isAdmin   = computed(() => user.value?.role === 'admin')
  const isTeacher = computed(() => user.value?.role === 'teacher' || user.value?.role === 'admin')
  const isStudent = computed(() => user.value?.role === 'student')

  function setUser(userData) {
    user.value = userData
  }

  async function fetchMe() {
    const { data } = await api.get('/auth/me')
    user.value = data
    return data
  }

  function logout() {
    user.value = null
    clearTokens()

    if (keycloak.authenticated) {
      keycloak.logout({ redirectUri: window.location.origin })
    } else {
      // Token dari bridge — cukup reload ke AccessDenied
      window.location.reload()
    }
  }

  return { user, isAuthenticated, isAdmin, isTeacher, isStudent, setUser, fetchMe, logout }
})
