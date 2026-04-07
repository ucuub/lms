<template>
  <Transition
    enter-active-class="transition ease-out duration-200"
    enter-from-class="opacity-0 -translate-y-2"
    enter-to-class="opacity-100 translate-y-0"
    leave-active-class="transition ease-in duration-150"
    leave-from-class="opacity-100"
    leave-to-class="opacity-0">
    <div v-if="message" :class="['alert', `alert-${type}`, 'flex items-start justify-between gap-3']">
      <span>{{ message }}</span>
      <button @click="$emit('dismiss')" class="shrink-0 opacity-60 hover:opacity-100">✕</button>
    </div>
  </Transition>
</template>

<script setup>
defineProps({
  message: String,
  type: { type: String, default: 'info' } // info | success | error
})
defineEmits(['dismiss'])
</script>
