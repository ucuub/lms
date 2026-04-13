<template>
  <div class="fixed bottom-4 right-4 z-50">
    <!-- Collapsed state — hanya tampil badge kecil -->
    <button v-if="collapsed"
      @click="collapsed = false"
      class="flex items-center gap-1.5 bg-yellow-400 text-yellow-900 text-xs font-semibold px-3 py-1.5 rounded-full shadow-lg hover:bg-yellow-300 transition">
      <span>⚡</span>
      {{ current?.name?.split(' ')[0] }}
      <span class="text-yellow-700 text-[10px]">({{ current?.role }})</span>
    </button>

    <!-- Expanded state -->
    <div v-else class="bg-white border border-yellow-400 rounded-xl shadow-xl p-3 w-52">
      <div class="flex items-center justify-between mb-2">
        <p class="text-xs font-semibold text-yellow-700 flex items-center gap-1">
          <span>⚡</span> Dev Login
        </p>
        <button @click="collapsed = true"
          class="text-gray-400 hover:text-gray-600 transition"
          title="Sembunyikan">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
          </svg>
        </button>
      </div>

      <div class="flex flex-col gap-1">
        <button
          v-for="u in users"
          :key="u.id"
          @click="switchUser(u)"
          :class="[
            'text-xs px-3 py-1.5 rounded-lg text-left transition-colors',
            current?.id === u.id
              ? 'bg-blue-600 text-white font-medium'
              : 'bg-gray-100 hover:bg-gray-200 text-gray-700'
          ]"
        >
          {{ u.name }}
          <span :class="['ml-1 text-[10px]', current?.id === u.id ? 'text-blue-200' : 'text-gray-400']">
            {{ u.role }}
          </span>
        </button>
      </div>

      <p v-if="loading" class="text-xs text-gray-400 mt-2 text-center">switching...</p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import api from '@/api/axios'

const auth = useAuthStore()
const loading = ref(false)
const collapsed = ref(false)

const users = [
  { id: 'seed-student-1', role: 'student', name: 'Budi (Student 1)' },
  { id: 'seed-student-2', role: 'student', name: 'Sari (Student 2)' },
  { id: 'seed-student-4', role: 'student', name: 'Nurul (Sertifikat)' },
  { id: 'seed-teacher-1', role: 'teacher', name: 'Dewi (Teacher 1)' },
  { id: 'seed-teacher-2', role: 'teacher', name: 'Bima (Teacher 2)' },
  { id: 'seed-admin-1',   role: 'admin',   name: 'Admin 1'          },
]

const stored = localStorage.getItem('mockUser')
const current = ref(stored ? JSON.parse(stored) : users[0])

async function switchUser(user) {
  if (loading.value || current.value?.id === user.id) return
  loading.value = true
  current.value = user
  localStorage.setItem('mockUser', JSON.stringify(user))
  try {
    const { data } = await api.post('/auth/sync')
    auth.setUser(data)
  } catch (e) {
    console.error('[MockUserSwitcher] sync failed', e)
  } finally {
    loading.value = false
  }
  window.location.reload()
}
</script>
