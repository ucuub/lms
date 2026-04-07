<template>
  <div v-if="totalPages > 1" class="flex items-center justify-center gap-1 mt-6">
    <button @click="$emit('change', modelValue - 1)" :disabled="modelValue === 1"
      class="px-3 py-1.5 rounded-lg text-sm font-medium border border-gray-200 disabled:opacity-40 hover:bg-gray-50 transition">
      ‹
    </button>

    <template v-for="p in pages" :key="p">
      <span v-if="p === '...'" class="px-3 py-1.5 text-gray-400 text-sm">…</span>
      <button v-else @click="$emit('change', p)"
        :class="['px-3 py-1.5 rounded-lg text-sm font-medium border transition',
                 p === modelValue ? 'bg-blue-600 text-white border-blue-600' : 'border-gray-200 hover:bg-gray-50']">
        {{ p }}
      </button>
    </template>

    <button @click="$emit('change', modelValue + 1)" :disabled="modelValue === totalPages"
      class="px-3 py-1.5 rounded-lg text-sm font-medium border border-gray-200 disabled:opacity-40 hover:bg-gray-50 transition">
      ›
    </button>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  modelValue: { type: Number, required: true },
  totalPages: { type: Number, required: true }
})
defineEmits(['change'])

const pages = computed(() => {
  const p = props.totalPages, c = props.modelValue
  if (p <= 7) return Array.from({ length: p }, (_, i) => i + 1)
  if (c <= 4) return [1, 2, 3, 4, 5, '...', p]
  if (c >= p - 3) return [1, '...', p-4, p-3, p-2, p-1, p]
  return [1, '...', c-1, c, c+1, '...', p]
})
</script>
