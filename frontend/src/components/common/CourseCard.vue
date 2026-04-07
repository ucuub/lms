<template>
  <RouterLink :to="`/courses/${course.id}`" class="card hover:shadow-md transition-shadow block">
    <!-- Thumbnail -->
    <div class="aspect-video bg-gradient-to-br from-blue-100 to-blue-50 overflow-hidden">
      <img v-if="course.thumbnailUrl"
           :src="course.thumbnailUrl"
           :alt="course.title"
           class="w-full h-full object-cover" />
      <div v-else class="w-full h-full flex items-center justify-center">
        <svg class="w-12 h-12 text-blue-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
        </svg>
      </div>
    </div>

    <div class="p-4">
      <!-- Badges -->
      <div class="flex items-center gap-2 mb-2 flex-wrap">
        <span v-if="course.category" class="badge-blue">{{ course.category }}</span>
        <span class="badge-gray">{{ course.level }}</span>
        <span v-if="!course.isPublished" class="badge-yellow">Draft</span>
      </div>

      <!-- Title -->
      <h3 class="font-semibold text-gray-900 line-clamp-2 mb-1">{{ course.title }}</h3>
      <p class="text-sm text-gray-500 mb-3">{{ course.instructorName }}</p>

      <!-- Stats -->
      <div class="flex items-center justify-between text-xs text-gray-500">
        <div class="flex items-center gap-1">
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"/>
          </svg>
          {{ course.enrollmentCount }} siswa
        </div>
        <div class="flex items-center gap-1">
          <svg class="w-3.5 h-3.5 fill-yellow-400 text-yellow-400" viewBox="0 0 24 24">
            <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
          </svg>
          {{ course.averageRating > 0 ? course.averageRating.toFixed(1) : '-' }}
        </div>
        <div class="flex items-center gap-1">
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
          </svg>
          {{ course.moduleCount }} modul
        </div>
      </div>

      <!-- Progress bar (if enrolled) -->
      <div v-if="progress !== undefined" class="mt-3">
        <div class="flex justify-between text-xs text-gray-500 mb-1">
          <span>Progress</span>
          <span>{{ progress }}%</span>
        </div>
        <div class="progress-bar">
          <div class="progress-fill" :style="`width: ${progress}%`"></div>
        </div>
      </div>
    </div>
  </RouterLink>
</template>

<script setup>
defineProps({
  course: { type: Object, required: true },
  progress: { type: Number, default: undefined }
})
</script>
