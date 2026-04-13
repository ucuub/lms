<template>
  <div class="flex h-full bg-white overflow-hidden" style="height: calc(100vh - 0px)">
    <!-- Conversation Sidebar -->
    <div class="w-72 border-r border-gray-200 flex flex-col shrink-0">
      <div class="p-4 border-b border-gray-100">
        <h2 class="font-semibold text-gray-900 text-lg">Pesan</h2>
      </div>

      <!-- New message button -->
      <div class="px-3 pt-3 pb-1">
        <button @click="showNewConv = true" class="btn-outline btn-sm w-full">
          + Pesan Baru
        </button>
      </div>

      <!-- Conversations list -->
      <div class="flex-1 overflow-y-auto">
        <div v-if="loadingConvs" class="flex justify-center py-8">
          <div class="animate-spin rounded-full h-6 w-6 border-2 border-blue-600 border-t-transparent"></div>
        </div>
        <div v-else-if="conversations.length === 0" class="text-center py-10 text-gray-400 text-sm px-4">
          Belum ada percakapan.
        </div>
        <button v-for="conv in conversations" :key="conv.id"
          @click="openConversation(conv)"
          :class="[
            'w-full text-left px-4 py-3 hover:bg-gray-50 transition border-b border-gray-50',
            activeConv?.id === conv.id ? 'bg-blue-50' : ''
          ]">
          <div class="flex items-center gap-3">
            <div class="w-9 h-9 rounded-full bg-blue-100 flex items-center justify-center shrink-0">
              <span class="text-blue-600 font-semibold text-sm">{{ initials(otherPerson(conv).name) }}</span>
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex items-center justify-between">
                <p class="text-sm font-medium text-gray-900 truncate">{{ otherPerson(conv).name }}</p>
                <span v-if="conv.unreadCount > 0"
                  class="w-5 h-5 bg-blue-600 text-white text-xs rounded-full flex items-center justify-center shrink-0 ml-1">
                  {{ conv.unreadCount > 9 ? '9+' : conv.unreadCount }}
                </span>
              </div>
              <p class="text-xs text-gray-400 truncate">{{ conv.lastMessage || 'Belum ada pesan' }}</p>
            </div>
          </div>
        </button>
      </div>
    </div>

    <!-- Chat Panel -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <!-- No conversation selected -->
      <div v-if="!activeConv" class="flex-1 flex flex-col items-center justify-center text-gray-400">
        <svg class="w-16 h-16 mb-3 text-gray-200" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"/>
        </svg>
        <p class="text-sm">Pilih percakapan atau mulai pesan baru</p>
      </div>

      <template v-else>
        <!-- Chat header -->
        <div class="flex items-center gap-3 px-5 py-4 border-b border-gray-200 bg-white">
          <div class="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center">
            <span class="text-blue-600 font-semibold text-sm">{{ initials(otherPerson(activeConv).name) }}</span>
          </div>
          <p class="font-medium text-gray-900">{{ otherPerson(activeConv).name }}</p>
        </div>

        <!-- Messages -->
        <div ref="msgContainer" class="flex-1 overflow-y-auto px-5 py-4 space-y-3">
          <div v-if="loadingMsgs" class="flex justify-center py-6">
            <div class="animate-spin rounded-full h-6 w-6 border-2 border-blue-600 border-t-transparent"></div>
          </div>
          <template v-else>
            <div v-if="messages.length === 0" class="text-center text-gray-400 text-sm py-6">
              Mulai percakapan dengan mengirimkan pesan.
            </div>
            <div v-for="msg in messages" :key="msg.id"
              :class="['flex items-end gap-2', msg.senderId === auth.user?.userId ? 'justify-end' : 'justify-start']">

              <div :class="[
                'max-w-xs lg:max-w-md px-4 py-2 rounded-2xl text-sm relative group',
                msg.senderId === auth.user?.userId
                  ? 'bg-blue-600 text-white rounded-br-sm'
                  : 'bg-gray-100 text-gray-800 rounded-bl-sm'
              ]">
                <p class="pr-5">{{ msg.content }}</p>
                <p :class="['text-xs mt-1', msg.senderId === auth.user?.userId ? 'text-blue-200' : 'text-gray-400']">
                  {{ formatTime(msg.createdAt) }}
                </p>

                <!-- Tombol hapus — hanya pesan sendiri -->
                <button v-if="msg.senderId === auth.user?.userId"
                  @click="deleteMessage(msg)"
                  class="absolute top-1.5 right-1.5 text-blue-300 hover:text-white transition-colors"
                  title="Hapus pesan">
                  ×
                </button>
              </div>
            </div>
          </template>
        </div>

        <!-- Input -->
        <div class="px-4 py-3 border-t border-gray-200 bg-white">
          <form @submit.prevent="sendMessage" class="flex items-center gap-2">
            <input v-model="newMessage" type="text" class="input flex-1"
              placeholder="Tulis pesan..." :disabled="sending" />
            <button type="submit" :disabled="sending || !newMessage.trim()" class="btn-primary px-4">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"/>
              </svg>
            </button>
          </form>
        </div>
      </template>
    </div>

    <!-- New Conversation Modal -->
    <div v-if="showNewConv" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showNewConv = false">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-sm p-6">
        <h3 class="font-semibold text-gray-900 mb-4">Pesan Baru</h3>
        <div class="space-y-3">
          <div>
            <label class="label">ID Pengguna</label>
            <input v-model="newRecipientId" type="text" class="input" placeholder="Masukkan user ID..." />
          </div>
          <div>
            <label class="label">Pesan</label>
            <textarea v-model="newFirstMsg" class="textarea" rows="3" placeholder="Tulis pesan pertama..."></textarea>
          </div>
        </div>
        <div class="flex gap-2 mt-4">
          <button @click="startNewConversation" :disabled="sending || !newRecipientId.trim() || !newFirstMsg.trim()" class="btn-primary flex-1">Kirim</button>
          <button @click="showNewConv = false" class="btn-outline">Batal</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { messagesApi } from '@/api/messages'

const auth = useAuthStore()

const conversations = ref([])
const activeConv = ref(null)
const messages = ref([])
const newMessage = ref('')
const loadingConvs = ref(true)
const loadingMsgs = ref(false)
const sending = ref(false)
const msgContainer = ref(null)

const showNewConv = ref(false)
const newRecipientId = ref('')
const newFirstMsg = ref('')

function initials(name) {
  return name?.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2) || '?'
}

function otherPerson(conv) {
  // ConversationDto sudah resolve otherUserId/otherUserName di backend
  return { id: conv.otherUserId, name: conv.otherUserName }
}

function formatTime(dt) {
  if (!dt) return ''
  const d = new Date(dt)
  const now = new Date()
  const isToday = d.toDateString() === now.toDateString()
  return isToday
    ? d.toLocaleTimeString('id-ID', { hour: '2-digit', minute: '2-digit' })
    : d.toLocaleDateString('id-ID', { day: 'numeric', month: 'short' })
}

async function loadConversations() {
  try {
    const { data } = await messagesApi.getConversations()
    conversations.value = data
  } finally {
    loadingConvs.value = false
  }
}

async function openConversation(conv) {
  activeConv.value = conv
  messages.value = []
  loadingMsgs.value = true
  try {
    const { data } = await messagesApi.getMessages(conv.id)
    messages.value = data
    await messagesApi.markRead(conv.id)
    conv.unreadCount = 0
    await nextTick()
    scrollToBottom()
  } finally {
    loadingMsgs.value = false
  }
}

function scrollToBottom() {
  if (msgContainer.value) {
    msgContainer.value.scrollTop = msgContainer.value.scrollHeight
  }
}

async function deleteMessage(msg) {
  if (!confirm('Hapus pesan ini?')) return
  try {
    await messagesApi.deleteMessage(msg.id)
    messages.value = messages.value.filter(m => m.id !== msg.id)
  } catch {
    alert('Gagal menghapus pesan.')
  }
}

async function sendMessage() {
  const content = newMessage.value.trim()
  if (!content || !activeConv.value) return
  sending.value = true
  try {
    const other = otherPerson(activeConv.value)
    const { data } = await messagesApi.send({ recipientId: other.id, content })
    messages.value.push(data)
    newMessage.value = ''
    await nextTick()
    scrollToBottom()
  } finally {
    sending.value = false
  }
}

async function startNewConversation() {
  sending.value = true
  try {
    await messagesApi.send({
      recipientId: newRecipientId.value.trim(),
      content: newFirstMsg.value.trim()
    })
    showNewConv.value = false
    newRecipientId.value = ''
    newFirstMsg.value = ''
    await loadConversations()
    // Buka conversation pertama (paling baru)
    if (conversations.value.length > 0) await openConversation(conversations.value[0])
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal mengirim pesan.')
  } finally {
    sending.value = false
  }
}

onMounted(loadConversations)
</script>
