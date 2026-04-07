<template>
  <div class="p-6 max-w-6xl mx-auto">
    <h1 class="text-2xl font-bold mb-6">Manajemen Kursus</h1>
    <input v-model="search" @input="debouncedLoad" class="input max-w-xs mb-4" placeholder="Cari kursus..." />
    <div class="table-wrap">
      <table>
        <thead><tr><th>Judul</th><th>Instruktur</th><th>Siswa</th><th>Status</th><th>Aksi</th></tr></thead>
        <tbody>
          <tr v-for="c in courses" :key="c.id">
            <td class="font-medium">{{ c.title }}</td>
            <td class="text-gray-500">{{ c.instructorName }}</td>
            <td>{{ c.enrollmentCount }}</td>
            <td><span :class="c.isPublished ? 'badge-green' : 'badge-yellow'" class="badge">{{ c.isPublished ? 'Publik' : 'Draft' }}</span></td>
            <td class="flex gap-2">
              <button @click="toggle(c)" class="btn-outline btn-sm">{{ c.isPublished ? 'Sembunyikan' : 'Publikasikan' }}</button>
              <button @click="del(c.id)" class="btn-danger btn-sm">Hapus</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <Pagination :model-value="page" :total-pages="totalPages" @change="p => { page = p; load() }" />
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { adminApi } from '@/api/forum'
import Pagination from '@/components/common/Pagination.vue'
const courses = ref([]), search = ref(''), page = ref(1), totalPages = ref(1)
let timer = null
function debouncedLoad() { clearTimeout(timer); timer = setTimeout(() => { page.value = 1; load() }, 400) }
async function load() {
  const { data } = await adminApi.getCourses({ search: search.value, page: page.value })
  courses.value = data.items; totalPages.value = data.totalPages
}
async function toggle(c) { const { data } = await adminApi.togglePublish(c.id); c.isPublished = data.isPublished }
async function del(id) {
  if (!confirm('Hapus kursus ini?')) return
  await adminApi.deleteCourse(id); load()
}
onMounted(load)
</script>
