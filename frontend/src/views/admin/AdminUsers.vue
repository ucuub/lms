<template>
  <div class="p-6 max-w-6xl mx-auto">
    <h1 class="text-2xl font-bold mb-6">Manajemen User</h1>
    <div class="flex gap-3 mb-4">
      <input v-model="search" @input="debouncedLoad" class="input max-w-xs" placeholder="Cari nama / email..." />
      <select v-model="roleFilter" @change="load" class="select w-36">
        <option value="">Semua Role</option>
        <option value="student">Student</option>
        <option value="teacher">Teacher</option>
        <option value="admin">Admin</option>
      </select>
    </div>
    <div class="table-wrap">
      <table>
        <thead><tr><th>Nama</th><th>Email</th><th>Role</th><th>Status</th><th>Aksi</th></tr></thead>
        <tbody>
          <tr v-for="u in users" :key="u.userId">
            <td class="font-medium">{{ u.name }}</td>
            <td class="text-gray-500">{{ u.email }}</td>
            <td>
              <select :value="u.role" @change="e => setRole(u.userId, e.target.value)" class="select py-1 text-xs w-28">
                <option value="student">Student</option>
                <option value="teacher">Teacher</option>
                <option value="admin">Admin</option>
              </select>
            </td>
            <td><span :class="u.isActive ? 'badge-green' : 'badge-red'" class="badge">{{ u.isActive ? 'Aktif' : 'Nonaktif' }}</span></td>
            <td><button @click="toggle(u)" class="btn-outline btn-sm">{{ u.isActive ? 'Nonaktifkan' : 'Aktifkan' }}</button></td>
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
const users = ref([]), search = ref(''), roleFilter = ref(''), page = ref(1), totalPages = ref(1)
let timer = null
function debouncedLoad() { clearTimeout(timer); timer = setTimeout(() => { page.value = 1; load() }, 400) }
async function load() {
  const { data } = await adminApi.getUsers({ search: search.value, role: roleFilter.value, page: page.value })
  users.value = data.items; totalPages.value = data.totalPages
}
async function setRole(userId, role) { await adminApi.setRole(userId, role); load() }
async function toggle(u) { const { data } = await adminApi.toggleActive(u.userId); u.isActive = data.isActive }
onMounted(load)
</script>
