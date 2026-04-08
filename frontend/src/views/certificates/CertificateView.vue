<template>
  <div class="p-6 max-w-2xl mx-auto">

    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Sertifikat & Penyelesaian</h1>
        <p class="text-gray-500 text-sm mt-0.5">{{ course?.title }}</p>
      </div>
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex flex-col items-center justify-center py-24 gap-3">
      <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
      <p class="text-gray-400 text-sm">Memuat data...</p>
    </div>

    <template v-else>

      <!-- ── SUDAH PUNYA SERTIFIKAT ─────────────────────────────────────── -->
      <div v-if="certificate" class="space-y-6">
        <!-- Achievement card -->
        <div class="card overflow-hidden">
          <div class="bg-gradient-to-br from-yellow-400 via-amber-400 to-orange-400 px-8 py-10 text-center relative">
            <!-- Decorative circles -->
            <div class="absolute top-4 left-4 w-16 h-16 rounded-full bg-white/10"></div>
            <div class="absolute bottom-4 right-4 w-24 h-24 rounded-full bg-white/10"></div>
            <div class="absolute top-1/2 right-8 w-8 h-8 rounded-full bg-white/10"></div>

            <div class="relative">
              <!-- Badge icon -->
              <div class="w-20 h-20 rounded-full bg-white/20 flex items-center justify-center mx-auto mb-4 ring-4 ring-white/30">
                <svg class="w-10 h-10 text-white" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 1l2.4 7.4H22l-6.2 4.5 2.4 7.4L12 16l-6.2 4.3 2.4-7.4L2 8.4h7.6z"/>
                </svg>
              </div>

              <h2 class="text-white font-bold text-2xl">Selamat! 🎉</h2>
              <p class="text-white/90 mt-1 text-sm">Kamu telah menyelesaikan kursus ini</p>

              <div class="mt-6 bg-white/20 backdrop-blur rounded-xl px-6 py-4 inline-block">
                <p class="text-white/70 text-xs uppercase tracking-widest mb-1">Nomor Sertifikat</p>
                <p class="text-white font-mono font-bold text-lg tracking-wider">{{ certificate.certificateNumber }}</p>
              </div>
            </div>
          </div>

          <div class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div class="bg-gray-50 rounded-lg p-4">
                <p class="text-xs text-gray-500 mb-1">Tanggal Diterbitkan</p>
                <p class="font-semibold text-gray-800">{{ formatDate(certificate.issuedAt) }}</p>
              </div>
              <div class="bg-gray-50 rounded-lg p-4">
                <p class="text-xs text-gray-500 mb-1">Nama Penerima</p>
                <p class="font-semibold text-gray-800">{{ certificate.userName }}</p>
              </div>
            </div>

            <!-- Verify link -->
            <div class="flex items-center gap-2 p-3 bg-green-50 rounded-lg border border-green-100">
              <svg class="w-4 h-4 text-green-600 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
              </svg>
              <p class="text-sm text-green-700">Sertifikat ini dapat diverifikasi oleh siapapun</p>
            </div>
          </div>
        </div>
      </div>

      <!-- ── BELUM PUNYA SERTIFIKAT ─────────────────────────────────────── -->
      <div v-else class="space-y-6">

        <!-- Completion status card -->
        <div class="card p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="font-semibold text-gray-900">Status Penyelesaian</h2>
            <span :class="status?.isCompleted ? 'badge-green' : 'badge-yellow'" class="badge">
              {{ status?.isCompleted ? 'Selesai' : 'Dalam Proses' }}
            </span>
          </div>

          <!-- Module progress -->
          <div class="mb-6">
            <div class="flex items-center justify-between mb-2">
              <span class="text-sm text-gray-600">Progress Modul</span>
              <span class="text-sm font-semibold text-gray-800">
                {{ status?.completedModules ?? 0 }} / {{ status?.totalModules ?? 0 }}
                <span class="text-gray-400 font-normal">({{ status?.percentage ?? 0 }}%)</span>
              </span>
            </div>
            <div class="h-3 bg-gray-100 rounded-full overflow-hidden">
              <div
                class="h-full rounded-full transition-all duration-700"
                :class="(status?.percentage ?? 0) >= 100 ? 'bg-green-500' : 'bg-blue-500'"
                :style="{ width: `${Math.min(status?.percentage ?? 0, 100)}%` }"
              ></div>
            </div>
          </div>

          <!-- Requirements checklist -->
          <div class="space-y-3">
            <p class="text-sm font-medium text-gray-700 mb-3">Persyaratan Kelulusan</p>

            <div
              v-for="req in requirements"
              :key="req.label"
              class="flex items-center gap-3 p-3 rounded-lg"
              :class="req.met ? 'bg-green-50' : 'bg-red-50'"
            >
              <div
                class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
                :class="req.met ? 'bg-green-500' : 'bg-red-400'"
              >
                <svg v-if="req.met" class="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                </svg>
                <svg v-else class="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </div>
              <div class="flex-1">
                <p class="text-sm font-medium" :class="req.met ? 'text-green-800' : 'text-red-800'">
                  {{ req.label }}
                </p>
                <p v-if="req.detail" class="text-xs mt-0.5" :class="req.met ? 'text-green-600' : 'text-red-600'">
                  {{ req.detail }}
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Claim button (hanya jika sudah selesai) -->
        <div v-if="status?.isCompleted" class="card p-6 text-center">
          <div class="mb-4">
            <div class="w-16 h-16 rounded-full bg-yellow-100 flex items-center justify-center mx-auto mb-3">
              <svg class="w-8 h-8 text-yellow-500" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 1l2.4 7.4H22l-6.2 4.5 2.4 7.4L12 16l-6.2 4.3 2.4-7.4L2 8.4h7.6z"/>
              </svg>
            </div>
            <h3 class="font-semibold text-gray-900">Kamu telah menyelesaikan kursus ini!</h3>
            <p class="text-gray-500 text-sm mt-1">Klaim sertifikatmu sekarang sebagai bukti pencapaian</p>
          </div>

          <button
            @click="claimCertificate"
            :disabled="claiming"
            class="btn-primary w-full justify-center"
          >
            <svg v-if="claiming" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            {{ claiming ? 'Memproses...' : 'Klaim Sertifikat' }}
          </button>
        </div>

        <!-- Belum selesai: motivasi -->
        <div v-else class="card p-6">
          <div class="flex gap-4">
            <div class="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center flex-shrink-0">
              <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
            </div>
            <div>
              <p class="font-medium text-gray-800">Selesaikan semua persyaratan</p>
              <p class="text-sm text-gray-500 mt-0.5">
                Kamu akan bisa klaim sertifikat setelah semua persyaratan di atas terpenuhi.
                Terus semangat!
              </p>
            </div>
          </div>
        </div>

      </div>
    </template>

    <!-- ── TOAST ─────────────────────────────────────────────────────────── -->
    <Transition name="toast">
      <div
        v-if="toast.show"
        class="fixed bottom-6 right-6 z-50 flex items-center gap-3 px-5 py-4 rounded-xl shadow-xl text-white"
        :class="toast.type === 'success' ? 'bg-green-600' : 'bg-red-600'"
      >
        <svg v-if="toast.type === 'success'" class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
        </svg>
        <svg v-else class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
        </svg>
        <p class="text-sm font-medium">{{ toast.message }}</p>
      </div>
    </Transition>

  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { coursesApi } from '@/api/courses'
import { certificatesApi } from '@/api/certificates'

const route = useRoute()
const courseId = Number(route.params.courseId)

// State
const loading  = ref(true)
const claiming = ref(false)
const course    = ref(null)
const status    = ref(null)
const rule      = ref(null)
const certificate = ref(null)

// Toast
const toast = ref({ show: false, type: 'success', message: '' })

function showToast(type, message) {
  toast.value = { show: true, type, message }
  setTimeout(() => { toast.value.show = false }, 4000)
}

// Requirements checklist derived dari rule + status
const requirements = computed(() => {
  if (!status.value || !rule.value) return []

  const list = []

  // Module percent
  const minPct = rule.value.requiredModulePercent ?? 100
  list.push({
    label: `Selesaikan minimal ${minPct}% modul`,
    detail: `Progress kamu: ${status.value.percentage}% (${status.value.completedModules}/${status.value.totalModules} modul)`,
    met: (status.value.percentage ?? 0) >= minPct
  })

  // Assignments
  if (rule.value.requireAllAssignments) {
    list.push({
      label: 'Kumpulkan semua tugas',
      detail: status.value.allAssignmentsSubmitted ? 'Semua tugas sudah dikumpulkan' : 'Ada tugas yang belum dikumpulkan',
      met: !!status.value.allAssignmentsSubmitted
    })
  }

  // Quizzes
  if (rule.value.requireAllQuizzesPassed) {
    list.push({
      label: 'Lulus semua quiz',
      detail: status.value.allQuizzesPassed ? 'Semua quiz sudah lulus' : 'Ada quiz yang belum lulus',
      met: !!status.value.allQuizzesPassed
    })
  }

  return list
})

async function claimCertificate() {
  claiming.value = true
  try {
    const { data } = await certificatesApi.claimCertificate(courseId)
    certificate.value = data
    showToast('success', `Sertifikat berhasil diterbitkan! No. ${data.certificateNumber}`)
  } catch (err) {
    const msg = err.response?.data?.message
      ?? err.response?.data?.detail
      ?? 'Gagal klaim sertifikat. Pastikan semua persyaratan sudah terpenuhi.'
    showToast('error', msg)
  } finally {
    claiming.value = false
  }
}

function formatDate(dateStr) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('id-ID', {
    day: 'numeric', month: 'long', year: 'numeric'
  })
}

onMounted(async () => {
  try {
    const [courseRes, statusRes, ruleRes] = await Promise.all([
      coursesApi.getById(courseId),
      certificatesApi.getCompletionStatus(courseId),
      certificatesApi.getCompletionRule(courseId),
    ])
    course.value = courseRes.data
    status.value = statusRes.data
    rule.value   = ruleRes.data

    // Cek apakah sudah punya sertifikat
    try {
      const certRes = await certificatesApi.getCertificate(courseId)
      certificate.value = certRes.data
    } catch {
      // 404 = belum punya, normal
    }
  } catch (err) {
    console.error('Failed to load completion data', err)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.btn-primary {
  @apply inline-flex items-center gap-2 px-5 py-2.5 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition disabled:opacity-60 disabled:cursor-not-allowed;
}
.btn-outline {
  @apply inline-flex items-center gap-2 px-4 py-2 border border-gray-200 text-sm font-medium rounded-lg hover:bg-gray-50 transition text-gray-700;
}
.btn-sm { @apply px-3 py-1.5 text-xs; }
.card   { @apply bg-white rounded-xl border border-gray-100 shadow-sm; }

.badge        { @apply inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium; }
.badge-green  { @apply bg-green-100 text-green-700; }
.badge-yellow { @apply bg-yellow-100 text-yellow-700; }
.badge-red    { @apply bg-red-100 text-red-700; }

/* Toast transition */
.toast-enter-active, .toast-leave-active { transition: all 0.3s ease; }
.toast-enter-from { opacity: 0; transform: translateY(1rem); }
.toast-leave-to   { opacity: 0; transform: translateY(1rem); }
</style>
