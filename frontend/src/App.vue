<template>
  <RouterView />
  <MockUserSwitcher v-if="isMockAuth" />
</template>

<script setup>
import MockUserSwitcher from '@/components/dev/MockUserSwitcher.vue'
const isMockAuth = import.meta.env.VITE_MOCK_AUTH === 'true'
</script>
