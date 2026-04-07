<template>
  <div class="min-h-screen bg-gray-50 flex items-center justify-center p-4">
    <div class="max-w-md w-full text-center">
      <div class="bg-white rounded-2xl shadow-sm border border-gray-200 p-10">
        <div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-6">
          <svg class="w-8 h-8 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M12 15v2m0 0v2m0-2h2m-2 0H10m2-5V9m0 0V7m0 2h2m-2 0H10M4.93 4.93l14.14 14.14" />
          </svg>
        </div>

        <h1 class="text-2xl font-bold text-gray-900 mb-2">Akses Ditolak</h1>
        <p class="text-gray-500 mb-8 leading-relaxed">
          LMS hanya dapat diakses melalui <strong class="text-gray-700">DWI Mobile</strong>.
          Silakan login terlebih dahulu melalui aplikasi DWI Mobile, kemudian buka LMS dari sana.
        </p>

        <div class="bg-blue-50 border border-blue-100 rounded-xl p-4 text-sm text-blue-700 text-left">
          <p class="font-semibold mb-1">Cara mengakses LMS:</p>
          <ol class="list-decimal list-inside space-y-1 text-blue-600">
            <li>Buka DWI Mobile</li>
            <li>Login dengan akun Anda</li>
            <li>Pilih menu <strong>LMS</strong></li>
          </ol>
        </div>
      </div>
    </div>
  </div>
</template>
