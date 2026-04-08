<template>
  <div class="p-6 max-w-3xl mx-auto">

    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Kalender</h1>
        <p class="text-gray-500 text-sm mt-0.5">Jadwal tugas, quiz, dan event kursusmu</p>
      </div>

      <!-- Month nav -->
      <div class="flex items-center gap-2">
        <button @click="prevMonth" class="btn-icon">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
          </svg>
        </button>
        <span class="text-sm font-semibold text-gray-700 min-w-[130px] text-center">
          {{ monthLabel }}
        </span>
        <button @click="nextMonth" class="btn-icon">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
          </svg>
        </button>
      </div>
    </div>

    <!-- Filter chips -->
    <div class="flex gap-2 mb-6 flex-wrap">
      <button
        v-for="f in filters"
        :key="f.value"
        @click="activeFilter = f.value"
        class="filter-chip"
        :class="activeFilter === f.value ? 'filter-chip-active' : 'filter-chip-default'"
      >
        <span class="w-2 h-2 rounded-full" :style="{ background: f.color }"></span>
        {{ f.label }}
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex flex-col items-center justify-center py-24 gap-3">
      <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
      <p class="text-gray-400 text-sm">Memuat kalender...</p>
    </div>

    <!-- Empty state -->
    <div v-else-if="groupedEvents.length === 0" class="flex flex-col items-center justify-center py-24 text-center">
      <div class="w-16 h-16 rounded-full bg-gray-100 flex items-center justify-center mb-4">
        <svg class="w-8 h-8 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
        </svg>
      </div>
      <p class="font-medium text-gray-500">Tidak ada event di bulan ini</p>
      <p class="text-sm text-gray-400 mt-1">Coba navigasi ke bulan lain atau ganti filter</p>
    </div>

    <!-- Event list grouped by date -->
    <div v-else class="space-y-6">
      <div v-for="group in groupedEvents" :key="group.dateKey">

        <!-- Date header -->
        <div class="flex items-center gap-3 sticky top-0 z-10 py-1 bg-gray-50 -mx-6 px-6">
          <div
            class="flex-shrink-0 w-12 h-12 rounded-xl flex flex-col items-center justify-center font-bold shadow-sm"
            :class="isToday(group.date) ? 'bg-blue-600 text-white' : 'bg-white border border-gray-200 text-gray-700'"
          >
            <span class="text-xs leading-none" :class="isToday(group.date) ? 'text-blue-200' : 'text-gray-400'">
              {{ shortDay(group.date) }}
            </span>
            <span class="text-lg leading-tight">{{ group.date.getDate() }}</span>
          </div>
          <div>
            <p class="font-semibold text-gray-800">{{ fullDate(group.date) }}</p>
            <p class="text-xs text-gray-400">{{ group.events.length }} event</p>
          </div>
        </div>

        <!-- Events for this date -->
        <div class="space-y-2 ml-0 sm:ml-16">
          <div
            v-for="ev in group.events"
            :key="ev.id"
            class="event-card"
            :style="{ '--accent': typeColor(ev.type) }"
          >
            <div class="event-accent"></div>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <span
                  class="event-badge"
                  :style="{ background: typeColor(ev.type) + '20', color: typeColor(ev.type) }"
                >
                  {{ typeIcon(ev.type) }} {{ typeLabel(ev.type) }}
                </span>
              </div>
              <p class="font-medium text-gray-800 text-sm leading-snug">{{ ev.title }}</p>
              <p v-if="ev.description" class="text-xs text-gray-500 mt-0.5 line-clamp-2">{{ ev.description }}</p>
            </div>
            <div class="text-right flex-shrink-0">
              <p class="text-xs font-medium" :style="{ color: typeColor(ev.type) }">
                {{ formatTime(ev.eventDate) }}
              </p>
            </div>
          </div>
        </div>

      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { calendarApi } from '@/api/certificates'

// ── State ──────────────────────────────────────────────────────────────────
const loading = ref(true)
const events  = ref([])
const activeFilter = ref('all')
const currentDate  = ref(new Date())

// ── Filters ────────────────────────────────────────────────────────────────
const filters = [
  { value: 'all',         label: 'Semua',     color: '#6b7280' },
  { value: 'Assignment',  label: 'Tugas',     color: '#ef4444' },
  { value: 'Quiz',        label: 'Quiz',      color: '#3b82f6' },
  { value: 'Event',       label: 'Event',     color: '#10b981' },
  { value: 'Announcement',label: 'Pengumuman',color: '#f59e0b' },
]

// ── Type helpers ───────────────────────────────────────────────────────────
function typeColor(type) {
  const map = {
    Assignment:   '#ef4444',
    Quiz:         '#3b82f6',
    Event:        '#10b981',
    Announcement: '#f59e0b',
  }
  return map[type] ?? '#6b7280'
}

function typeIcon(type) {
  const map = {
    Assignment:   '📝',
    Quiz:         '❓',
    Event:        '📅',
    Announcement: '📢',
  }
  return map[type] ?? '•'
}

function typeLabel(type) {
  const map = {
    Assignment:   'Tugas',
    Quiz:         'Quiz',
    Event:        'Event',
    Announcement: 'Pengumuman',
  }
  return map[type] ?? type
}

// ── Month navigation ───────────────────────────────────────────────────────
const monthLabel = computed(() =>
  currentDate.value.toLocaleDateString('id-ID', { month: 'long', year: 'numeric' })
)

function prevMonth() {
  const d = new Date(currentDate.value)
  d.setDate(1)
  d.setMonth(d.getMonth() - 1)
  currentDate.value = d
}

function nextMonth() {
  const d = new Date(currentDate.value)
  d.setDate(1)
  d.setMonth(d.getMonth() + 1)
  currentDate.value = d
}

// ── Computed: filter → group by date ──────────────────────────────────────
const filteredEvents = computed(() => {
  if (activeFilter.value === 'all') return events.value
  return events.value.filter(e => e.type === activeFilter.value)
})

const groupedEvents = computed(() => {
  const map = new Map()

  for (const ev of filteredEvents.value) {
    const d = new Date(ev.eventDate)
    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`

    if (!map.has(key)) {
      map.set(key, {
        dateKey: key,
        date: new Date(d.getFullYear(), d.getMonth(), d.getDate()),
        events: []
      })
    }
    map.get(key).events.push(ev)
  }

  return [...map.values()].sort((a, b) => a.date - b.date)
})

// ── Date formatting ────────────────────────────────────────────────────────
function isToday(date) {
  const today = new Date()
  return date.getDate() === today.getDate()
    && date.getMonth() === today.getMonth()
    && date.getFullYear() === today.getFullYear()
}

function shortDay(date) {
  return date.toLocaleDateString('id-ID', { weekday: 'short' }).slice(0, 3)
}

function fullDate(date) {
  return date.toLocaleDateString('id-ID', { weekday: 'long', day: 'numeric', month: 'long' })
}

function formatTime(dateStr) {
  const d = new Date(dateStr)
  const h = d.getHours(), m = d.getMinutes()
  // If midnight-ish (00:00), just show "Jatuh Tempo"
  if (h === 0 && m === 0) return 'Jatuh Tempo'
  return d.toLocaleTimeString('id-ID', { hour: '2-digit', minute: '2-digit' })
}

// ── Fetch ──────────────────────────────────────────────────────────────────
async function fetchEvents() {
  loading.value = true
  try {
    const d = currentDate.value
    const from = new Date(d.getFullYear(), d.getMonth(), 1).toISOString()
    const to   = new Date(d.getFullYear(), d.getMonth() + 1, 0, 23, 59, 59).toISOString()

    const { data } = await calendarApi.getEvents(from, to)
    events.value = data
  } catch (err) {
    console.error('Failed to load calendar events', err)
    events.value = []
  } finally {
    loading.value = false
  }
}

watch(currentDate, fetchEvents)
onMounted(fetchEvents)
</script>

<style scoped>
/* Buttons */
.btn-icon {
  @apply w-8 h-8 rounded-lg border border-gray-200 flex items-center justify-center text-gray-600 hover:bg-gray-100 hover:border-gray-300 transition;
}

/* Filter chips */
.filter-chip {
  @apply inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium border transition cursor-pointer;
}
.filter-chip-active  { @apply bg-blue-600 text-white border-blue-600; }
.filter-chip-default { @apply bg-white text-gray-600 border-gray-200 hover:border-gray-300 hover:bg-gray-50; }

/* Event card */
.event-card {
  @apply flex items-start gap-4 bg-white rounded-xl border border-gray-100 shadow-sm p-4 relative overflow-hidden hover:shadow-md transition;
}
.event-accent {
  @apply absolute left-0 top-0 bottom-0 w-1 rounded-l-xl;
  background: var(--accent);
}
.event-badge {
  @apply inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold;
}

/* Sticky date header background */
.sticky { background: #f9fafb; }
</style>
