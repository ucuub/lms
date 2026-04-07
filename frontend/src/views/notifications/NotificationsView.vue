<template>
  <div class="max-w-2xl mx-auto p-6">
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold">Notifikasi</h1>
      <button @click="markAll" class="btn-outline btn-sm">Tandai semua dibaca</button>
    </div>
    <div class="space-y-2">
      <div v-for="n in notifications" :key="n.id"
        :class="['card p-4 cursor-pointer transition', n.isRead ? 'opacity-70' : 'border-l-4 border-l-blue-500']"
        @click="markRead(n)">
        <div class="flex items-start justify-between">
          <div><p class="font-medium text-sm">{{ n.title }}</p><p class="text-sm text-gray-600">{{ n.message }}</p></div>
          <span class="text-xs text-gray-400 ml-4 shrink-0">{{ new Date(n.createdAt).toLocaleDateString('id-ID') }}</span>
        </div>
      </div>
      <div v-if="notifications.length === 0" class="card p-12 text-center text-gray-400">Tidak ada notifikasi.</div>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { notificationsApi } from '@/api/forum'
import { useNotificationStore } from '@/stores/notifications'
const notifStore = useNotificationStore()
const notifications = ref([])
async function load() {
  const { data } = await notificationsApi.getAll()
  notifications.value = data.items
}
async function markRead(n) {
  if (!n.isRead) { await notificationsApi.markRead(n.id); n.isRead = true; notifStore.fetchCount() }
}
async function markAll() {
  await notificationsApi.markAllRead(); notifications.value.forEach(n => n.isRead = true); notifStore.fetchCount()
}
onMounted(load)
</script>
