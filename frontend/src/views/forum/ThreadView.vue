<template>
  <div class="max-w-3xl mx-auto p-6">
    <button @click="$router.back()" class="btn-outline btn-sm mb-4">← Forum</button>
    <div v-if="thread" class="space-y-4">
      <div class="card p-6">
        <h1 class="text-xl font-bold mb-1">{{ thread.title }}</h1>
        <p class="text-sm text-gray-500 mb-4">{{ thread.userName }} · {{ new Date(thread.createdAt).toLocaleDateString('id-ID') }}</p>
        <p class="text-gray-700 whitespace-pre-line">{{ thread.body }}</p>
      </div>
      <div class="card p-6">
        <h2 class="font-semibold mb-4">{{ thread.replies?.length ?? 0 }} Balasan</h2>
        <div class="space-y-4 mb-6">
          <div v-for="r in thread.replies" :key="r.id" class="border-l-2 border-blue-200 pl-4">
            <p class="text-xs text-gray-500 mb-1">{{ r.userName }} · {{ new Date(r.createdAt).toLocaleDateString('id-ID') }}</p>
            <p class="text-sm text-gray-800 whitespace-pre-line">{{ r.body }}</p>
          </div>
        </div>
        <form @submit.prevent="reply" class="space-y-2">
          <textarea v-model="replyText" class="textarea" rows="3" placeholder="Tulis balasan..." required></textarea>
          <button type="submit" class="btn-primary btn-sm">Kirim Balasan</button>
        </form>
      </div>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { forumApi } from '@/api/forum'
const route = useRoute()
const thread = ref(null), replyText = ref('')
async function load() {
  const { data } = await forumApi.getThread(route.params.courseId, route.params.threadId)
  thread.value = data
}
async function reply() {
  await forumApi.reply(route.params.courseId, route.params.threadId, { body: replyText.value })
  replyText.value = ''
  load()
}
onMounted(load)
</script>
