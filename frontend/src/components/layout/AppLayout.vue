<template>
  <div class="flex h-screen bg-gray-50 overflow-hidden">
    <!-- Sidebar -->
    <aside class="w-64 bg-white border-r border-gray-200 flex flex-col shrink-0">
      <!-- Logo -->
      <div class="px-6 py-5 border-b border-gray-100">
        <RouterLink to="/" class="flex items-center gap-2">
          <div class="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
            <span class="text-white font-bold text-sm">L</span>
          </div>
          <span class="font-bold text-gray-900 text-lg">LMS Pro</span>
        </RouterLink>
      </div>

      <!-- Nav -->
      <nav class="flex-1 overflow-y-auto px-3 py-4 space-y-1">
        <RouterLink to="/dashboard" class="sidebar-link">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/></svg>
          Dashboard
        </RouterLink>

        <RouterLink to="/courses" class="sidebar-link">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/></svg>
          Kursus
        </RouterLink>

        <RouterLink to="/notifications" class="sidebar-link">
          <div class="relative">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/></svg>
            <span v-if="notifCount > 0"
              class="absolute -top-1 -right-1 w-4 h-4 bg-red-500 text-white text-xs rounded-full flex items-center justify-center">
              {{ notifCount > 9 ? '9+' : notifCount }}
            </span>
          </div>
          Notifikasi
        </RouterLink>

        <template v-if="auth.isTeacher">
          <div class="pt-3 pb-1">
            <p class="px-4 text-xs font-semibold text-gray-400 uppercase tracking-wider">Pengajaran</p>
          </div>
          <RouterLink to="/courses/create" class="sidebar-link">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
            Buat Kursus
          </RouterLink>
          <RouterLink to="/question-bank" class="sidebar-link">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/></svg>
            Bank Soal
          </RouterLink>
        </template>

        <template v-if="auth.isAdmin">
          <div class="pt-3 pb-1">
            <p class="px-4 text-xs font-semibold text-gray-400 uppercase tracking-wider">Admin</p>
          </div>
          <RouterLink to="/admin" class="sidebar-link">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/></svg>
            Dashboard Admin
          </RouterLink>
          <RouterLink to="/admin/users" class="sidebar-link">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"/></svg>
            Manajemen User
          </RouterLink>
          <RouterLink to="/admin/courses" class="sidebar-link">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"/></svg>
            Semua Kursus
          </RouterLink>
        </template>
      </nav>

      <!-- User Profile -->
      <div class="px-3 py-4 border-t border-gray-100">
        <div class="flex items-center gap-3 px-4 py-2">
          <div class="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center shrink-0">
            <img v-if="auth.user?.avatarUrl" :src="auth.user.avatarUrl" class="w-8 h-8 rounded-full object-cover" />
            <span v-else class="text-blue-600 font-semibold text-sm">{{ initials }}</span>
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-gray-900 truncate">{{ auth.user?.name }}</p>
            <p class="text-xs text-gray-500 capitalize">{{ auth.user?.role }}</p>
          </div>
          <button @click="handleLogout" class="text-gray-400 hover:text-gray-600 transition-colors" title="Keluar">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/></svg>
          </button>
        </div>
      </div>
    </aside>

    <!-- Main content -->
    <main class="flex-1 overflow-y-auto">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notifications'

const auth = useAuthStore()
const notifStore = useNotificationStore()
const router = useRouter()

const initials = computed(() =>
  auth.user?.name?.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2) || 'U'
)
const notifCount = computed(() => notifStore.unreadCount)

let pollTimer
onMounted(() => { pollTimer = notifStore.startPolling() })
onUnmounted(() => clearInterval(pollTimer))

async function handleLogout() {
  await auth.logout()
  router.push('/login')
}
</script>
