<template>
  <div class="p-6 max-w-7xl mx-auto">
    <!-- Header -->
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-gray-900">Dashboard</h1>
      <p class="text-gray-500 mt-1">Selamat datang kembali, <span class="font-medium text-gray-700">{{ auth.user?.name }}</span></p>
    </div>

    <!-- Student Dashboard -->
    <template v-if="auth.isStudent">
      <!-- Stats -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <div class="card p-5">
          <p class="text-sm text-gray-500">Kursus Aktif</p>
          <p class="text-3xl font-bold text-blue-600 mt-1">{{ stats.activeCourses }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Selesai</p>
          <p class="text-3xl font-bold text-green-600 mt-1">{{ stats.completedCourses }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Tugas Pending</p>
          <p class="text-3xl font-bold text-yellow-600 mt-1">{{ stats.pendingAssignments }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Sertifikat</p>
          <p class="text-3xl font-bold text-purple-600 mt-1">{{ stats.certificates }}</p>
        </div>
      </div>

      <!-- My Courses -->
      <div class="mb-8">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-lg font-semibold text-gray-900">Kursus Saya</h2>
          <RouterLink to="/courses" class="text-sm text-blue-600 hover:underline">Jelajahi kursus →</RouterLink>
        </div>

        <div v-if="loading" class="flex justify-center py-12">
          <div class="animate-spin rounded-full h-8 w-8 border-2 border-blue-600 border-t-transparent"></div>
        </div>

        <div v-else-if="myCourses.length === 0" class="card p-12 text-center">
          <svg class="w-12 h-12 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
          </svg>
          <p class="text-gray-500">Belum ada kursus. <RouterLink to="/courses" class="text-blue-600 hover:underline">Mulai belajar sekarang!</RouterLink></p>
        </div>

        <div v-else class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <CourseCard v-for="course in myCourses" :key="course.id" :course="course" :progress="progressMap[course.id]" />
        </div>
      </div>
    </template>

    <!-- Teacher Dashboard -->
    <template v-else-if="auth.isTeacher && !auth.isAdmin">
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <div class="card p-5">
          <p class="text-sm text-gray-500">Kursus Saya</p>
          <p class="text-3xl font-bold text-blue-600 mt-1">{{ stats.totalCourses }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Total Siswa</p>
          <p class="text-3xl font-bold text-green-600 mt-1">{{ stats.totalStudents }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Quiz Aktif</p>
          <p class="text-3xl font-bold text-yellow-600 mt-1">{{ stats.activeQuizzes }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Tugas Pending</p>
          <p class="text-3xl font-bold text-red-600 mt-1">{{ stats.ungradedSubmissions }}</p>
        </div>
      </div>

      <div class="flex items-center justify-between mb-4">
        <h2 class="text-lg font-semibold text-gray-900">Kursus Saya</h2>
        <RouterLink to="/courses/create" class="btn-primary btn-sm">+ Buat Kursus</RouterLink>
      </div>

      <div v-if="myCourses.length === 0" class="card p-12 text-center">
        <p class="text-gray-500">Belum ada kursus. <RouterLink to="/courses/create" class="text-blue-600 hover:underline">Buat sekarang!</RouterLink></p>
      </div>
      <div v-else class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <CourseCard v-for="course in myCourses" :key="course.id" :course="course" />
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { coursesApi } from '@/api/courses'
import CourseCard from '@/components/common/CourseCard.vue'

const auth = useAuthStore()
const loading = ref(true)
const myCourses = ref([])
const progressMap = reactive({})
const stats = reactive({
  activeCourses: 0, completedCourses: 0, pendingAssignments: 0, certificates: 0,
  totalCourses: 0, totalStudents: 0, activeQuizzes: 0, ungradedSubmissions: 0
})

onMounted(async () => {
  try {
    const { data } = await coursesApi.getMy()
    myCourses.value = data

    if (auth.isStudent) {
      stats.activeCourses = data.length
    } else {
      stats.totalCourses = data.length
      stats.totalStudents = data.reduce((sum, c) => sum + c.enrollmentCount, 0)
    }
  } finally {
    loading.value = false
  }
})
</script>
