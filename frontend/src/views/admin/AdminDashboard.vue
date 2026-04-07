<template>
  <div class="p-6 max-w-7xl mx-auto">
    <h1 class="text-2xl font-bold mb-6">Dashboard Admin</h1>
    <div v-if="stats" class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
      <div class="card p-5"><p class="text-sm text-gray-500">Total User</p><p class="text-3xl font-bold text-blue-600">{{ stats.totalUsers }}</p></div>
      <div class="card p-5"><p class="text-sm text-gray-500">Total Kursus</p><p class="text-3xl font-bold text-green-600">{{ stats.totalCourses }}</p></div>
      <div class="card p-5"><p class="text-sm text-gray-500">Total Enrollment</p><p class="text-3xl font-bold text-yellow-600">{{ stats.totalEnrollments }}</p></div>
      <div class="card p-5"><p class="text-sm text-gray-500">Sertifikat</p><p class="text-3xl font-bold text-purple-600">{{ stats.totalCertificates }}</p></div>
    </div>
    <div class="grid lg:grid-cols-2 gap-6">
      <div class="card p-6">
        <h2 class="font-semibold mb-4">Top 10 Kursus</h2>
        <div class="space-y-2">
          <div v-for="c in stats?.topCourses" :key="c.id" class="flex items-center justify-between text-sm">
            <span class="text-gray-700 truncate flex-1">{{ c.title }}</span>
            <span class="text-gray-500 ml-2 shrink-0">{{ c.enrollmentCount }} siswa</span>
          </div>
        </div>
      </div>
      <div class="card p-6">
        <h2 class="font-semibold mb-4">Navigasi Cepat</h2>
        <div class="space-y-2">
          <RouterLink to="/admin/users" class="btn-outline w-full justify-start">👥 Manajemen User</RouterLink>
          <RouterLink to="/admin/courses" class="btn-outline w-full justify-start">📚 Manajemen Kursus</RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { adminApi } from '@/api/forum'
const stats = ref(null)
onMounted(async () => { const { data } = await adminApi.getStats(); stats.value = data })
</script>
