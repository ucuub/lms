<template>
  <div class="p-6 max-w-7xl mx-auto">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Katalog Kursus</h1>
        <p class="text-gray-500 text-sm mt-1">{{ pagination.totalCount }} kursus tersedia</p>
      </div>
      <RouterLink v-if="auth.isTeacher" to="/courses/create" class="btn-primary">+ Buat Kursus</RouterLink>
    </div>

    <div class="flex gap-6">
      <!-- Sidebar Filter -->
      <aside class="w-52 shrink-0 hidden lg:block">
        <div class="card p-4 sticky top-6 space-y-5">
          <!-- Search -->
          <div>
            <label class="label">Cari</label>
            <input v-model="filters.search" @input="debouncedFetch" type="text" class="input" placeholder="Judul, instruktur..." />
          </div>

          <!-- Category -->
          <div>
            <label class="label">Kategori</label>
            <div class="space-y-1">
              <button @click="setFilter('category', '')" :class="['w-full text-left text-sm px-2 py-1 rounded transition', !filters.category ? 'bg-blue-50 text-blue-700 font-medium' : 'hover:bg-gray-50']">
                Semua
              </button>
              <button v-for="cat in categories" :key="cat"
                @click="setFilter('category', cat)"
                :class="['w-full text-left text-sm px-2 py-1 rounded transition', filters.category === cat ? 'bg-blue-50 text-blue-700 font-medium' : 'hover:bg-gray-50']">
                {{ cat }}
              </button>
            </div>
          </div>

          <!-- Level -->
          <div>
            <label class="label">Level</label>
            <div class="space-y-1">
              <button v-for="lv in levels" :key="lv" @click="setFilter('level', lv === 'Semua' ? '' : lv)"
                :class="['w-full text-left text-sm px-2 py-1 rounded transition',
                         (lv === 'Semua' ? !filters.level : filters.level === lv) ? 'bg-blue-50 text-blue-700 font-medium' : 'hover:bg-gray-50']">
                {{ lv }}
              </button>
            </div>
          </div>

          <!-- Sort -->
          <div>
            <label class="label">Urutkan</label>
            <select v-model="filters.sort" @change="fetchCourses" class="select">
              <option value="newest">Terbaru</option>
              <option value="oldest">Terlama</option>
              <option value="popular">Terpopuler</option>
              <option value="rating">Rating Tertinggi</option>
            </select>
          </div>
        </div>
      </aside>

      <!-- Course Grid -->
      <div class="flex-1">
        <div v-if="loading" class="flex justify-center py-20">
          <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
        </div>

        <div v-else-if="courses.length === 0" class="card p-16 text-center">
          <svg class="w-16 h-16 text-gray-200 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          <p class="text-gray-400">Tidak ada kursus ditemukan.</p>
        </div>

        <div v-else class="grid sm:grid-cols-2 xl:grid-cols-3 gap-5">
          <CourseCard v-for="course in courses" :key="course.id" :course="course" />
        </div>

        <Pagination :model-value="filters.page" :total-pages="pagination.totalPages" @change="p => { filters.page = p; fetchCourses() }" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { coursesApi } from '@/api/courses'
import CourseCard from '@/components/common/CourseCard.vue'
import Pagination from '@/components/common/Pagination.vue'

const auth = useAuthStore()
const courses = ref([])
const categories = ref([])
const levels = ['Semua', 'Pemula', 'Menengah', 'Lanjutan']
const loading = ref(false)

const filters = reactive({ search: '', category: '', level: '', sort: 'newest', page: 1, pageSize: 12 })
const pagination = reactive({ totalCount: 0, totalPages: 1 })

let debounceTimer = null
function debouncedFetch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => { filters.page = 1; fetchCourses() }, 400)
}

function setFilter(key, val) {
  filters[key] = val
  filters.page = 1
  fetchCourses()
}

async function fetchCourses() {
  loading.value = true
  try {
    const params = { ...filters }
    if (!params.category) delete params.category
    if (!params.level) delete params.level
    const { data } = await coursesApi.getAll(params)
    courses.value = data.items
    pagination.totalCount = data.totalCount
    pagination.totalPages = data.totalPages
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  fetchCourses()
  const { data } = await coursesApi.getCategories()
  categories.value = data
})
</script>
