<template>
  <div class="relative" v-click-outside="close">
    <div class="relative">
      <svg class="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
      </svg>
      <input
        ref="inputRef"
        v-model="query"
        @input="onInput"
        @focus="open = query.length > 1"
        @keydown.escape="close"
        @keydown.down.prevent="moveDown"
        @keydown.up.prevent="moveUp"
        @keydown.enter.prevent="selectHighlighted"
        type="text"
        class="w-full pl-9 pr-3 py-2 text-sm border border-gray-200 rounded-lg bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white transition"
        placeholder="Cari kursus, modul, forum..."
      />
      <div v-if="loading" class="absolute right-3 top-1/2 -translate-y-1/2">
        <div class="animate-spin rounded-full h-3 w-3 border-2 border-blue-500 border-t-transparent"></div>
      </div>
    </div>

    <!-- Dropdown results -->
    <div v-if="open && (results.length > 0 || (query.length > 1 && !loading))"
      class="absolute top-full left-0 mt-1 w-full min-w-[320px] bg-white border border-gray-200 rounded-xl shadow-lg z-50 overflow-hidden">

      <div v-if="results.length === 0" class="px-4 py-3 text-sm text-gray-400">
        Tidak ditemukan hasil untuk "{{ query }}"
      </div>

      <template v-else>
        <template v-for="(group, type) in grouped" :key="type">
          <div class="px-3 pt-2 pb-1">
            <p class="text-xs font-semibold text-gray-400 uppercase tracking-wider">{{ groupLabel(type) }}</p>
          </div>
          <button v-for="(item, idx) in group" :key="item.id"
            @click="navigate(item)"
            :class="[
              'w-full text-left px-4 py-2 hover:bg-blue-50 transition flex items-center gap-3',
              highlightedIdx === globalIdx(type, idx) ? 'bg-blue-50' : ''
            ]">
            <span class="text-lg shrink-0">{{ typeIcon(type) }}</span>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-800 truncate">{{ item.title }}</p>
              <p class="text-xs text-gray-400 truncate">{{ item.subtitle }}</p>
            </div>
          </button>
        </template>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { searchApi } from '@/api/search'

const router = useRouter()
const query = ref('')
const results = ref([])
const loading = ref(false)
const open = ref(false)
const highlightedIdx = ref(-1)
let debounceTimer

const grouped = computed(() => {
  const g = {}
  for (const r of results.value) {
    if (!g[r.type]) g[r.type] = []
    g[r.type].push(r)
  }
  return g
})

const flatResults = computed(() => results.value)

function globalIdx(type, idx) {
  const types = Object.keys(grouped.value)
  let total = 0
  for (const t of types) {
    if (t === type) return total + idx
    total += grouped.value[t].length
  }
  return -1
}

function groupLabel(type) {
  return { course: 'Kursus', module: 'Modul', forum: 'Forum' }[type] ?? type
}

function typeIcon(type) {
  return { course: '📚', module: '📄', forum: '💬' }[type] ?? '🔍'
}

function onInput() {
  clearTimeout(debounceTimer)
  highlightedIdx.value = -1
  if (query.value.length < 2) { open.value = false; results.value = []; return }
  debounceTimer = setTimeout(doSearch, 250)
}

async function doSearch() {
  loading.value = true
  try {
    const { data } = await searchApi.search(query.value)
    results.value = data
    open.value = true
  } catch {
    results.value = []
  } finally {
    loading.value = false
  }
}

function close() {
  open.value = false
  highlightedIdx.value = -1
}

function moveDown() {
  if (!open.value) return
  highlightedIdx.value = Math.min(highlightedIdx.value + 1, flatResults.value.length - 1)
}

function moveUp() {
  highlightedIdx.value = Math.max(highlightedIdx.value - 1, -1)
}

function selectHighlighted() {
  if (highlightedIdx.value >= 0 && flatResults.value[highlightedIdx.value]) {
    navigate(flatResults.value[highlightedIdx.value])
  }
}

function navigate(item) {
  close()
  query.value = ''
  results.value = []
  if (item.type === 'course') router.push(`/courses/${item.id}`)
  else if (item.type === 'module') router.push(`/courses/${item.courseId}/modules/${item.id}`)
  else if (item.type === 'forum') router.push(`/courses/${item.courseId}/forum`)
}

// v-click-outside directive
const vClickOutside = {
  mounted(el, { value }) {
    el._clickOutsideHandler = (e) => {
      if (!el.contains(e.target)) value()
    }
    document.addEventListener('click', el._clickOutsideHandler)
  },
  unmounted(el) {
    document.removeEventListener('click', el._clickOutsideHandler)
  }
}
</script>
