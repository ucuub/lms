import { defineStore } from 'pinia'
import { ref } from 'vue'
import { notificationsApi } from '@/api/forum'

export const useNotificationStore = defineStore('notifications', () => {
  const unreadCount = ref(0)

  async function fetchCount() {
    try {
      const { data } = await notificationsApi.getCount()
      unreadCount.value = data.count
    } catch {}
  }

  function startPolling(intervalMs = 60000) {
    fetchCount()
    return setInterval(fetchCount, intervalMs)
  }

  return { unreadCount, fetchCount, startPolling }
})
