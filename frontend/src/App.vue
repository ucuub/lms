<template>
  <RouterView />
  <ToastContainer />
  <MockUserSwitcher v-if="isMockAuth" />
</template>

<script setup>
import MockUserSwitcher from '@/components/dev/MockUserSwitcher.vue'
import ToastContainer from '@/components/common/ToastContainer.vue'
const isMockAuth = import.meta.env.VITE_MOCK_AUTH === 'true'
</script>
