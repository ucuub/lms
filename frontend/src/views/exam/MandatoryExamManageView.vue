<template>
  <div class="p-6 max-w-5xl mx-auto">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Ujian Wajib</h1>
        <p class="text-sm text-gray-500 mt-0.5">Kelola ujian berbasis deep link per user</p>
      </div>
      <button @click="showCreate = true"
        class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
        </svg>
        Buat Ujian Wajib
      </button>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreate" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg p-6">
        <h3 class="font-semibold text-lg mb-4">Buat Ujian Wajib</h3>
        <div class="space-y-3">
          <div>
            <label class="label">Judul *</label>
            <input v-model="form.title" class="input" placeholder="Judul ujian" />
          </div>
          <div>
            <label class="label">Deskripsi</label>
            <textarea v-model="form.description" class="textarea" rows="2" placeholder="Opsional"></textarea>
          </div>
          <div class="grid grid-cols-3 gap-3">
            <div>
              <label class="label">Batas Waktu (menit)</label>
              <input v-model.number="form.timeLimitMinutes" type="number" class="input" placeholder="Kosongkan = tanpa batas" min="1" />
            </div>
            <div>
              <label class="label">Maks Percobaan</label>
              <input v-model.number="form.maxAttempts" type="number" class="input" min="1" />
            </div>
            <div>
              <label class="label">Nilai Lulus (%)</label>
              <input v-model.number="form.passScore" type="number" class="input" min="0" max="100" />
            </div>
          </div>
          <div>
            <label class="label">Soal per Peserta</label>
            <input v-model.number="form.questionsPerAttempt" type="number" class="input" placeholder="Kosongkan = tampilkan semua soal" min="1" />
            <p class="text-xs text-gray-400 mt-1">Sistem akan acak sejumlah soal ini dari total soal yang tersedia.</p>
          </div>
        </div>
        <div class="flex gap-2 mt-5">
          <button @click="createExam" :disabled="saving" class="btn-primary btn-sm flex-1">
            {{ saving ? 'Menyimpan...' : 'Simpan' }}
          </button>
          <button @click="showCreate = false" class="btn-outline btn-sm flex-1">Batal</button>
        </div>
      </div>
    </div>

    <!-- Edit Modal -->
    <div v-if="showEdit" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg p-6">
        <h3 class="font-semibold text-lg mb-4">Edit Ujian</h3>
        <div class="space-y-3">
          <div>
            <label class="label">Judul *</label>
            <input v-model="editForm.title" class="input" placeholder="Judul ujian" />
          </div>
          <div>
            <label class="label">Deskripsi</label>
            <textarea v-model="editForm.description" class="textarea" rows="2" placeholder="Opsional"></textarea>
          </div>
          <div class="grid grid-cols-3 gap-3">
            <div>
              <label class="label">Batas Waktu (menit)</label>
              <input v-model.number="editForm.timeLimitMinutes" type="number" class="input" placeholder="Tanpa batas" min="1" />
            </div>
            <div>
              <label class="label">Maks Percobaan</label>
              <input v-model.number="editForm.maxAttempts" type="number" class="input" min="1" />
            </div>
            <div>
              <label class="label">Nilai Lulus (%)</label>
              <input v-model.number="editForm.passScore" type="number" class="input" min="0" max="100" />
            </div>
          </div>
          <div>
            <label class="label">Webhook URL (DWI Mobile)</label>
            <input v-model="editForm.webhookUrl" class="input" placeholder="https://dwimobile.example.com/webhook (opsional)" />
          </div>
          <div>
            <label class="label">Soal per Peserta</label>
            <input v-model.number="editForm.questionsPerAttempt" type="number" class="input" placeholder="Kosongkan = tampilkan semua soal" min="1" />
            <p class="text-xs text-gray-400 mt-1">Sistem akan acak sejumlah soal ini dari total soal yang tersedia.</p>
          </div>
        </div>
        <div class="flex gap-2 mt-5">
          <button @click="saveEdit" :disabled="editSaving" class="btn-primary btn-sm flex-1">
            {{ editSaving ? 'Menyimpan...' : 'Simpan Perubahan' }}
          </button>
          <button @click="showEdit = false" class="btn-outline btn-sm flex-1">Batal</button>
        </div>
      </div>
    </div>

    <!-- List -->
    <div v-if="loading" class="text-center py-12 text-gray-400">Memuat...</div>
    <div v-else-if="exams.length === 0" class="text-center py-16 text-gray-400">
      <p>Belum ada ujian wajib.</p>
    </div>
    <div v-else class="space-y-4">
      <div v-for="exam in exams" :key="exam.id"
        class="bg-white rounded-xl border border-gray-200 p-5">
        <div class="flex items-start justify-between gap-4 mb-3">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-0.5 flex-wrap">
              <h3 class="font-semibold text-gray-900 truncate">{{ exam.title }}</h3>
              <span :class="exam.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'"
                class="text-xs font-medium px-2 py-0.5 rounded-full shrink-0">
                {{ exam.isActive ? 'Aktif' : 'Nonaktif' }}
              </span>
              <!-- Exam ID badge — untuk diberikan ke tim DWI Mobile -->
              <button
                @click="copyExamId(exam.id)"
                class="text-xs font-mono bg-blue-50 text-blue-600 border border-blue-200 px-2 py-0.5 rounded-full hover:bg-blue-100 transition-colors"
                :title="`Klik untuk salin Exam ID`">
                ID: {{ exam.id }} 📋
              </button>
            </div>
            <p v-if="exam.description" class="text-sm text-gray-500 line-clamp-1">{{ exam.description }}</p>
          </div>
          <div class="flex gap-2 shrink-0">
            <button @click="openDetail(exam)"
              class="text-xs px-3 py-1.5 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
              Kelola
            </button>
            <button @click="openEdit(exam)"
              class="text-xs px-3 py-1.5 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
              Edit
            </button>
            <button @click="toggleActive(exam)"
              class="text-xs px-3 py-1.5 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
              {{ exam.isActive ? 'Nonaktifkan' : 'Aktifkan' }}
            </button>
            <button @click="exportResults(exam)"
              class="text-xs px-3 py-1.5 border border-green-200 text-green-600 rounded-lg hover:bg-green-50 transition-colors">
              Export CSV
            </button>
            <button @click="deleteExam(exam)"
              class="text-xs px-3 py-1.5 border border-red-200 text-red-500 rounded-lg hover:bg-red-50 transition-colors">
              Hapus
            </button>
          </div>
        </div>
        <div class="flex gap-4 text-xs text-gray-500 mb-3">
          <span>{{ exam.questionCount }} soal</span>
          <template v-if="exam.questionsPerAttempt">
            <span>·</span>
            <span class="text-blue-600 font-medium">{{ exam.questionsPerAttempt }} diacak</span>
          </template>
          <span>·</span>
          <span>{{ exam.timeLimitMinutes ? `${exam.timeLimitMinutes} menit` : 'Tanpa batas waktu' }}</span>
          <span>·</span>
          <span>Lulus ≥{{ exam.passScore }}%</span>
          <span>·</span>
          <span>{{ exam.assignmentCount }} peserta</span>
        </div>

        <!-- Public Link Section -->
        <div v-if="exam.publicLink" class="bg-green-50 border border-green-200 rounded-lg p-3">
          <div class="flex items-center justify-between gap-2 mb-1">
            <span class="text-xs font-medium text-green-700">Link Publik (DWI Mobile)</span>
            <div class="flex gap-1.5">
              <button @click="copyPublicLink(exam.publicLink)"
                class="text-xs text-green-600 hover:text-green-800 border border-green-300 px-2 py-0.5 rounded">
                Salin Link
              </button>
              <button @click="revokePublicLink(exam)"
                class="text-xs text-red-500 hover:text-red-700 border border-red-200 px-2 py-0.5 rounded hover:bg-red-50">
                Cabut
              </button>
            </div>
          </div>
          <p class="text-xs font-mono text-green-800 break-all">{{ exam.publicLink }}&amp;userId={USER_ID}&amp;userName={USER_NAME}</p>
          <p class="text-xs text-green-600 mt-1">DWI Mobile ganti <code class="bg-green-100 px-1 rounded">{USER_ID}</code> dan <code class="bg-green-100 px-1 rounded">{USER_NAME}</code> dengan data user mereka.</p>
        </div>
        <div v-else class="flex items-center gap-2">
          <button @click="generatePublicLink(exam)"
            :disabled="!exam.isActive || generatingCode === exam.id"
            class="text-xs px-3 py-1.5 border border-blue-200 text-blue-600 rounded-lg hover:bg-blue-50 transition-colors disabled:opacity-50">
            {{ generatingCode === exam.id ? 'Generating...' : '🔗 Generate Link Publik' }}
          </button>
          <span v-if="!exam.isActive" class="text-xs text-gray-400">Aktifkan exam terlebih dahulu</span>
        </div>
      </div>
    </div>

    <!-- AI Generate Modal -->
    <AiGenerateModal v-if="showAiModal"
      :model="aiModel"
      :provider-name="aiProviderName"
      @close="showAiModal = false"
      @save="onAiSave" />

    <!-- Import from Question Bank Modal -->
    <div v-if="showImport" class="fixed inset-0 z-[60] flex items-center justify-center bg-black/50 p-4">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl flex flex-col max-h-[90vh]">
        <!-- Header -->
        <div class="flex items-center justify-between p-5 border-b shrink-0">
          <div>
            <h3 class="font-semibold text-gray-900">Import dari Bank Soal</h3>
            <p class="text-xs text-gray-400 mt-0.5">Soal akan disalin ke ujian wajib "{{ selected?.title }}"</p>
          </div>
          <button @click="showImport = false" class="text-gray-400 hover:text-gray-600">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <!-- Course selector -->
        <div class="p-5 border-b shrink-0">
          <label class="label">Pilih Kursus</label>
          <select v-model="importCourseId" @change="onImportCourseChange" class="select">
            <option :value="null" disabled>-- Pilih kursus --</option>
            <option v-for="c in importCourses" :key="c.id" :value="c.id">{{ c.title }}</option>
          </select>
          <p v-if="importCourses.length === 0" class="text-xs text-gray-400 mt-1">Tidak ada kursus yang bisa diakses.</p>
        </div>

        <!-- Question list -->
        <div class="flex-1 overflow-y-auto p-5">
          <div v-if="!importCourseId" class="text-center py-10 text-gray-400 text-sm">
            Pilih kursus terlebih dahulu.
          </div>
          <div v-else-if="importBankLoading" class="text-center py-10 text-gray-400 text-sm">Memuat soal...</div>
          <div v-else-if="importBankItems.length === 0" class="text-center py-10 text-gray-400 text-sm">
            Kursus ini belum memiliki soal di bank soal.
          </div>
          <div v-else>
            <!-- Filter + select all -->
            <div class="flex items-center justify-between gap-3 mb-3 flex-wrap">
              <div class="flex gap-1.5">
                <button v-for="f in ['Semua', 'PG', 'TF', 'Essay']" :key="f"
                  @click="setImportTypeFilter(f)"
                  :class="importTypeFilter === f ? 'bg-blue-600 text-white' : 'border border-gray-200 text-gray-600 hover:bg-gray-50'"
                  class="text-xs px-3 py-1 rounded-full transition-colors">
                  {{ f }}
                </button>
              </div>
              <label class="flex items-center gap-2 text-sm text-gray-600 cursor-pointer select-none">
                <input type="checkbox" :checked="importAllChecked" @change="toggleImportAll" class="accent-blue-600" />
                Pilih semua ({{ importFiltered.length }})
              </label>
            </div>

            <!-- Questions -->
            <div class="space-y-2">
              <label v-for="q in importFiltered" :key="q.id"
                class="flex items-start gap-3 p-3 border rounded-lg cursor-pointer transition-colors"
                :class="importSelected.has(q.id) ? 'border-blue-400 bg-blue-50' : 'border-gray-200 hover:border-gray-300'">
                <input type="checkbox" :checked="importSelected.has(q.id)"
                  @change="toggleImportSelect(q.id)" class="mt-0.5 accent-blue-600 shrink-0" />
                <div class="flex-1 min-w-0">
                  <p class="text-sm text-gray-800 line-clamp-2">{{ q.text }}</p>
                  <div class="flex gap-2 mt-1 flex-wrap">
                    <span class="text-xs px-1.5 py-0.5 rounded bg-gray-100 text-gray-500">
                      {{ { MultipleChoice: 'Pilihan Ganda', TrueFalse: 'Benar/Salah', Essay: 'Essay' }[q.type] ?? q.type }}
                    </span>
                    <span class="text-xs text-gray-400">{{ q.points }} poin</span>
                    <span v-if="q.moduleName" class="text-xs text-blue-500">{{ q.moduleName }}</span>
                  </div>
                </div>
              </label>
            </div>
          </div>
        </div>

        <!-- Footer -->
        <div class="p-5 border-t shrink-0 flex items-center justify-between gap-3">
          <p class="text-sm text-gray-500">
            <span v-if="importSelected.size > 0" class="font-medium text-blue-600">{{ importSelected.size }} soal dipilih</span>
            <span v-else class="text-gray-400">Belum ada soal dipilih</span>
          </p>
          <div class="flex gap-2">
            <button @click="showImport = false" class="btn-outline btn-sm">Batal</button>
            <button @click="doImport" :disabled="importSelected.size === 0 || importing"
              class="btn-primary btn-sm">
              {{ importing ? 'Mengimpor...' : `Import ${importSelected.size > 0 ? importSelected.size + ' Soal' : ''}` }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Detail Drawer -->
    <div v-if="selected" class="fixed inset-0 z-50 flex">
      <div class="flex-1 bg-black/40" @click="selected = null" />
      <div class="w-full max-w-2xl bg-white flex flex-col overflow-hidden shadow-xl">
        <div class="flex items-center justify-between p-5 border-b">
          <h3 class="font-semibold text-gray-900">{{ selected.title }}</h3>
          <button @click="selected = null" class="text-gray-400 hover:text-gray-600">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <div class="flex-1 overflow-y-auto">
          <!-- Tabs -->
          <div class="flex border-b overflow-x-auto">
            <button v-for="tab in ['Soal', 'Peserta', 'Hasil', 'Generate Link', 'Riwayat Link']" :key="tab"
              @click="activeTab = tab; if (tab === 'Riwayat Link') loadSessions(); if (tab === 'Hasil') loadAttempts()"
              :class="activeTab === tab ? 'border-b-2 border-blue-600 text-blue-600' : 'text-gray-500'"
              class="px-5 py-3 text-sm font-medium transition-colors whitespace-nowrap">
              {{ tab }}
            </button>
          </div>

          <!-- Tab: Soal -->
          <div v-if="activeTab === 'Soal'" class="p-5">
            <!-- Add Question -->
            <div class="flex gap-2 mb-4 flex-wrap">
              <button @click="showAddQ = !showAddQ" class="btn-outline btn-sm">
                {{ showAddQ ? 'Batal' : '+ Tambah Soal' }}
              </button>
              <button @click="openImportModal" class="btn-outline btn-sm text-blue-600 border-blue-300 hover:bg-blue-50">
                ↓ Import dari Bank Soal
              </button>
              <button v-if="isAdmin" @click="showAiModal = true"
                class="inline-flex items-center gap-1 text-sm px-3 py-1 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors">
                ✨ Generate AI
              </button>
            </div>
            <div v-if="showAddQ" class="card p-4 mb-4">
              <div class="space-y-3">
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="label">Tipe</label>
                    <select v-model="qForm.type" @change="onQTypeChange" class="select">
                      <option value="MultipleChoice">Pilihan Ganda</option>
                      <option value="TrueFalse">Benar/Salah</option>
                      <option value="Essay">Essay</option>
                    </select>
                  </div>
                  <div>
                    <label class="label">Poin</label>
                    <input v-model.number="qForm.points" type="number" class="input" min="1" />
                  </div>
                </div>
                <div>
                  <label class="label">Pertanyaan</label>
                  <textarea v-model="qForm.text" class="textarea" rows="2"></textarea>
                </div>
                <div v-if="qForm.type !== 'Essay'">
                  <label class="label">Opsi</label>
                  <div v-for="(o, i) in qForm.options" :key="i" class="flex gap-2 mb-2">
                    <input type="radio" :checked="o.isCorrect"
                      @change="qForm.options.forEach((x, j) => x.isCorrect = j === i)" class="mt-2.5" />
                    <input v-model="o.text" class="input" :placeholder="`Opsi ${i + 1}`" />
                    <button v-if="qForm.type === 'MultipleChoice'" type="button"
                      @click="qForm.options.splice(i, 1)" class="text-red-400 hover:text-red-600">✕</button>
                  </div>
                  <button v-if="qForm.type === 'MultipleChoice'" type="button"
                    @click="qForm.options.push({ text: '', isCorrect: false })" class="btn-outline btn-sm">+ Opsi</button>
                </div>
                <button @click="addQuestion" :disabled="savingQ" class="btn-primary btn-sm">
                  {{ savingQ ? 'Menyimpan...' : 'Simpan Soal' }}
                </button>
              </div>
            </div>

            <div v-if="detailLoading" class="text-gray-400 text-sm">Memuat...</div>
            <div v-else-if="detail?.questions?.length === 0" class="text-gray-400 text-sm">Belum ada soal.</div>
            <div v-else class="space-y-2">
              <div v-for="(q, idx) in detail?.questions" :key="q.id" class="border rounded-lg overflow-hidden">

                <!-- Soal normal (bukan mode edit) -->
                <div v-if="editingQId !== q.id" class="p-3 flex items-start justify-between gap-3">
                  <div class="flex-1 min-w-0">
                    <p class="text-sm text-gray-800">{{ idx + 1 }}. {{ q.text }}</p>
                    <p class="text-xs text-gray-400 mt-1">{{ q.type }} · {{ q.points }} poin</p>
                    <div v-if="q.options?.length" class="flex flex-wrap gap-1 mt-1">
                      <span v-for="o in q.options" :key="o.id"
                        :class="['text-xs px-1.5 py-0.5 rounded', o.isCorrect ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500']">
                        {{ o.text }}
                      </span>
                    </div>
                  </div>
                  <div class="flex items-center gap-1 shrink-0">
                    <!-- Reorder -->
                    <button @click="moveQuestion(idx, -1)" :disabled="idx === 0 || reordering"
                      class="text-gray-400 hover:text-gray-600 disabled:opacity-30 px-1 text-lg leading-none" title="Naik">↑</button>
                    <button @click="moveQuestion(idx, 1)" :disabled="idx === detail.questions.length - 1 || reordering"
                      class="text-gray-400 hover:text-gray-600 disabled:opacity-30 px-1 text-lg leading-none" title="Turun">↓</button>
                    <!-- Edit -->
                    <button @click="openEditQ(q)" class="text-xs text-blue-500 hover:text-blue-700 px-2 py-0.5 border border-blue-200 rounded hover:bg-blue-50">Edit</button>
                    <!-- Delete -->
                    <button @click="deleteQuestion(q.id)" class="text-xs text-red-400 hover:text-red-600 px-2 py-0.5 border border-red-200 rounded hover:bg-red-50">Hapus</button>
                  </div>
                </div>

                <!-- Inline edit form -->
                <div v-else class="p-3 bg-blue-50 border-t border-blue-200 space-y-2">
                  <div>
                    <label class="label text-xs">Pertanyaan</label>
                    <textarea v-model="editQForm.text" class="textarea text-sm" rows="2"></textarea>
                  </div>
                  <div class="flex gap-2 items-center">
                    <label class="label text-xs mb-0">Poin</label>
                    <input v-model.number="editQForm.points" type="number" min="1" class="input w-20 text-sm" />
                  </div>
                  <!-- Opsi (hanya untuk non-Essay) -->
                  <div v-if="q.type !== 'Essay'">
                    <label class="label text-xs">Opsi</label>
                    <div v-for="(o, oi) in editQForm.options" :key="oi" class="flex gap-2 mb-1.5 items-center">
                      <input type="radio" :checked="o.isCorrect"
                        @change="editQForm.options.forEach((x, j) => x.isCorrect = j === oi)"
                        class="shrink-0 accent-blue-600" />
                      <input v-model="o.text" class="input text-sm flex-1" :placeholder="`Opsi ${oi + 1}`" />
                      <button v-if="q.type === 'MultipleChoice'" @click="editQForm.options.splice(oi, 1)"
                        class="text-red-400 hover:text-red-600 text-xs shrink-0">✕</button>
                    </div>
                    <button v-if="q.type === 'MultipleChoice'" @click="editQForm.options.push({ text: '', isCorrect: false })"
                      class="text-xs text-blue-600 hover:underline">+ Tambah opsi</button>
                  </div>
                  <div class="flex gap-2 pt-1">
                    <button @click="saveEditQ(q)" :disabled="savingQEdit"
                      class="btn-primary btn-sm">{{ savingQEdit ? 'Menyimpan...' : 'Simpan' }}</button>
                    <button @click="editingQId = null" class="btn-outline btn-sm">Batal</button>
                  </div>
                </div>

              </div>
            </div>
          </div>

          <!-- Tab: Peserta -->
          <div v-if="activeTab === 'Peserta'" class="p-5">
            <!-- Assign form -->
            <div class="flex gap-2 mb-4">
              <input v-model="assignUserId" class="input flex-1" placeholder="User ID" />
              <input v-model="assignUserName" class="input flex-1" placeholder="Nama user" />
              <button @click="assignUser" :disabled="assigning" class="btn-primary btn-sm shrink-0">
                {{ assigning ? '...' : 'Assign' }}
              </button>
            </div>
            <div v-if="detailLoading" class="text-gray-400 text-sm">Memuat...</div>
            <div v-else-if="detail?.assignments?.length === 0" class="text-gray-400 text-sm">Belum ada peserta.</div>
            <table v-else class="w-full text-sm">
              <thead>
                <tr class="text-left text-xs text-gray-500 border-b">
                  <th class="pb-2">User</th>
                  <th class="pb-2">Status</th>
                  <th class="pb-2">Percobaan</th>
                  <th class="pb-2"></th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="a in detail?.assignments" :key="a.id" class="py-2">
                  <td class="py-2 font-medium">{{ a.userName }}<br/><span class="text-xs text-gray-400">{{ a.userId }}</span></td>
                  <td class="py-2">
                    <span :class="{
                      'bg-gray-100 text-gray-500': a.status === 'NotYet',
                      'bg-yellow-100 text-yellow-700': a.status === 'InProgress',
                      'bg-green-100 text-green-700': a.status === 'Done',
                    }" class="text-xs px-2 py-0.5 rounded-full font-medium">
                      {{ { NotYet: 'Belum', InProgress: 'Sedang', Done: 'Selesai' }[a.status] }}
                    </span>
                  </td>
                  <td class="py-2 text-gray-500">{{ a.attemptCount }}</td>
                  <td class="py-2">
                    <button @click="unassignUser(a.userId)" class="text-xs text-red-400 hover:text-red-600">Hapus</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Tab: Hasil -->
          <div v-if="activeTab === 'Hasil'" class="p-5">
            <div class="flex items-center justify-between mb-4">
              <p class="text-sm text-gray-500">Hasil ujian semua peserta yang sudah submit.</p>
              <div class="flex gap-2">
                <button @click="exportCSV" class="text-xs px-3 py-1 border border-green-300 text-green-600 rounded-lg hover:bg-green-50 transition-colors">
                  ↓ Export CSV
                </button>
                <button @click="loadAttempts" class="text-xs text-blue-600 hover:underline">Refresh</button>
              </div>
            </div>

            <div v-if="attemptsLoading" class="text-gray-400 text-sm">Memuat...</div>
            <div v-else-if="attempts.length === 0" class="text-center py-10 text-gray-400 text-sm">Belum ada yang submit.</div>
            <div v-else class="space-y-3">
              <div v-for="att in attempts" :key="att.id"
                class="border rounded-xl overflow-hidden"
                :class="att.isPassed ? 'border-green-200' : 'border-red-200'">

                <!-- Attempt header -->
                <div class="flex items-center justify-between gap-3 p-3"
                  :class="att.isPassed ? 'bg-green-50' : 'bg-red-50'">
                  <div>
                    <p class="text-sm font-medium text-gray-800">{{ att.userName }}</p>
                    <p class="text-xs text-gray-400">{{ att.userId }}</p>
                  </div>
                  <div class="text-right shrink-0">
                    <div class="flex items-center gap-2">
                      <span class="text-lg font-bold" :class="att.isPassed ? 'text-green-600' : 'text-red-500'">
                        {{ att.percentage }}%
                      </span>
                      <span class="text-xs font-medium px-2 py-0.5 rounded-full"
                        :class="att.isPassed ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'">
                        {{ att.isPassed ? 'Lulus' : 'Belum Lulus' }}
                      </span>
                    </div>
                    <p class="text-xs text-gray-400">{{ att.score }} / {{ att.maxScore }} poin</p>
                    <p class="text-xs text-gray-400">{{ formatDate(att.submittedAt) }}</p>
                  </div>
                </div>

                <!-- Essay grading (if any) -->
                <div v-if="att.essayAnswers?.length" class="p-3 border-t border-gray-100 space-y-3">
                  <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Jawaban Essay</p>
                  <div v-for="ea in att.essayAnswers" :key="ea.answerId" class="bg-gray-50 rounded-lg p-3">
                    <p class="text-xs font-medium text-gray-700 mb-1">{{ ea.questionText }}</p>
                    <p class="text-xs text-gray-500 bg-white border rounded p-2 mb-2 whitespace-pre-wrap">
                      {{ ea.essayAnswer || '(tidak dijawab)' }}
                    </p>
                    <div class="flex items-center gap-2 flex-wrap">
                      <div class="flex items-center gap-1">
                        <label class="text-xs text-gray-500">Nilai:</label>
                        <input
                          v-model.number="grading[ea.answerId].points"
                          type="number" :min="0" :max="ea.maxPoints"
                          class="w-16 border border-gray-300 rounded px-2 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-blue-400" />
                        <span class="text-xs text-gray-400">/ {{ ea.maxPoints }}</span>
                      </div>
                      <input
                        v-model="grading[ea.answerId].feedback"
                        class="flex-1 min-w-32 border border-gray-300 rounded px-2 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-blue-400"
                        :placeholder="`Feedback (opsional)`" />
                      <button
                        @click="saveGrade(att, ea)"
                        :disabled="grading[ea.answerId].saving"
                        class="text-xs px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 transition-colors">
                        {{ grading[ea.answerId].saving ? '...' : 'Simpan' }}
                      </button>
                      <span v-if="grading[ea.answerId].saved" class="text-xs text-green-600">✓ Tersimpan</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Tab: Riwayat Link -->
          <div v-if="activeTab === 'Riwayat Link'" class="p-5">
            <div class="flex justify-between items-center mb-4">
              <p class="text-sm text-gray-600">Semua link yang pernah digenerate untuk ujian ini.</p>
              <button @click="loadSessions" class="text-xs text-blue-600 hover:underline">Refresh</button>
            </div>
            <div v-if="sessionsLoading" class="text-gray-400 text-sm">Memuat...</div>
            <div v-else-if="sessions.length === 0" class="text-gray-400 text-sm">Belum ada link yang digenerate.</div>
            <div v-else class="space-y-2">
              <div v-for="s in sessions" :key="s.id"
                class="p-3 border rounded-lg text-xs flex items-start justify-between gap-3"
                :class="s.isRevoked ? 'bg-red-50 border-red-200' : s.isExpired ? 'bg-gray-50 border-gray-200' : 'border-green-200 bg-green-50'">
                <div class="space-y-0.5">
                  <div class="flex items-center gap-2">
                    <span :class="s.isRevoked ? 'bg-red-100 text-red-700' : s.isExpired ? 'bg-gray-200 text-gray-500' : 'bg-green-100 text-green-700'"
                      class="px-1.5 py-0.5 rounded font-medium">
                      {{ s.isRevoked ? 'Dicabut' : s.isExpired ? 'Expired' : 'Aktif' }}
                    </span>
                    <span class="text-gray-600">User: <b>{{ s.userId }}</b></span>
                  </div>
                  <p class="text-gray-400">Generate: {{ formatDate(s.generatedAt) }}</p>
                  <p class="text-gray-400">Berlaku sampai: {{ formatDate(s.expiresAt) }}</p>
                  <p class="text-gray-400">
                    Pertama dipakai: {{ s.usedAt ? formatDate(s.usedAt) : 'Belum pernah dipakai' }}
                  </p>
                </div>
                <button v-if="!s.isRevoked && !s.isExpired"
                  @click="revokeSession(s)"
                  class="text-red-500 hover:text-red-700 shrink-0 border border-red-200 px-2 py-1 rounded hover:bg-red-50">
                  Cabut
                </button>
              </div>
            </div>
          </div>

          <!-- Tab: Generate Link -->
          <div v-if="activeTab === 'Generate Link'" class="p-5">
            <!-- Info box: integrasi DWI Mobile -->
            <div class="mb-5 p-4 bg-blue-50 border border-blue-200 rounded-xl text-sm">
              <p class="font-semibold text-blue-800 mb-1">Integrasi Otomatis (DWI Mobile)</p>
              <p class="text-blue-700 mb-2">
                DWI Mobile dapat generate link secara otomatis tanpa intervensi admin. Berikan informasi berikut ke tim DWI Mobile:
              </p>
              <div class="space-y-1.5">
                <div class="flex items-center gap-2">
                  <span class="text-blue-600 font-medium w-24 shrink-0">Exam ID:</span>
                  <code class="bg-white border border-blue-200 px-2 py-0.5 rounded font-mono text-blue-700">{{ selected?.id }}</code>
                  <button @click="copyExamId(selected?.id)" class="text-xs text-blue-500 hover:text-blue-700 border border-blue-200 px-2 py-0.5 rounded">Salin</button>
                </div>
                <div class="flex items-center gap-2">
                  <span class="text-blue-600 font-medium w-24 shrink-0">Endpoint:</span>
                  <code class="bg-white border border-blue-200 px-2 py-0.5 rounded font-mono text-blue-700 text-xs break-all">POST /api/service/mandatory-exams/{{ selected?.id }}/generate-link</code>
                </div>
                <div class="flex items-center gap-2">
                  <span class="text-blue-600 font-medium w-24 shrink-0">Auth:</span>
                  <code class="bg-white border border-blue-200 px-2 py-0.5 rounded font-mono text-xs text-blue-700">X-Api-Key: &lt;kunci dari admin server&gt;</code>
                </div>
              </div>
            </div>

            <p class="text-sm text-gray-600 mb-2">
              Generate link yang bisa dibagikan ke siapa saja. Penerima link cukup masukkan nama mereka saat membuka link.
            </p>
            <div class="mb-4 p-3 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-800">
              <b>Perhatian:</b> Setiap link hanya dapat digunakan oleh maksimal <b>5 peserta berbeda</b>. Jika sudah lulus, peserta tidak dapat mengakses link kembali. Jika lebih dari 5 peserta membutuhkan akses, generate link baru.
            </div>
            <div class="space-y-3 mb-4">
              <div>
                <label class="label">Berlaku selama (menit)</label>
                <input v-model.number="linkExpiry" type="number" class="input w-32" min="10" max="1440" />
                <p class="text-xs text-gray-400 mt-1">Maksimal 1440 menit (24 jam)</p>
              </div>
              <button @click="generateLink" :disabled="generatingLink" class="btn-primary btn-sm">
                {{ generatingLink ? 'Generating...' : 'Generate Link' }}
              </button>
            </div>

            <div v-if="generatedLink" class="p-4 bg-gray-50 rounded-xl border border-gray-200">
              <p class="text-xs text-gray-500 mb-1">Deep Link:</p>
              <div class="flex gap-2 items-center">
                <p class="text-xs font-mono text-blue-600 flex-1 break-all">{{ generatedLink.deepLink }}</p>
                <button @click="copyLink" class="text-xs text-gray-500 hover:text-gray-700 shrink-0 border px-2 py-1 rounded">
                  Salin
                </button>
              </div>
              <p class="text-xs text-gray-400 mt-2">Berlaku hingga: {{ formatDate(generatedLink.expiresAt) }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { mandatoryExamApi } from '@/api/mandatoryExam'
import { coursesApi } from '@/api/courses'
import { courseQuestionBankApi } from '@/api/courseQuestionBank'
import { aiApi } from '@/api/ai'
import AiGenerateModal from '@/components/AiGenerateModal.vue'

const exams   = ref([])
const loading = ref(true)
const saving  = ref(false)
const showCreate = ref(false)

const form = reactive({
  title: '', description: '', timeLimitMinutes: null, maxAttempts: 1, passScore: 60, questionsPerAttempt: null
})

// Edit exam
const showEdit  = ref(false)
const editTarget = ref(null)
const editForm  = reactive({
  title: '', description: '', timeLimitMinutes: null, maxAttempts: 1, passScore: 60, webhookUrl: '', questionsPerAttempt: null
})
const editSaving = ref(false)

// Detail drawer
const selected    = ref(null)
const detail      = ref(null)
const detailLoading = ref(false)
const activeTab   = ref('Soal')

// Add question
const showAddQ = ref(false)
const savingQ  = ref(false)
const qForm    = reactive({
  type: 'MultipleChoice', text: '', points: 10,
  options: [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
})

// Edit question
const editingQId = ref(null)
const editQForm  = reactive({ text: '', points: 10, options: [] })
const savingQEdit = ref(false)

// Reorder
const reordering = ref(false)

// Assign
const assignUserId   = ref('')
const assignUserName = ref('')
const assigning      = ref(false)

// Generate link
const linkUserId      = ref('')
const linkExpiry      = ref(60)
const generatingLink  = ref(false)
const generatedLink   = ref(null)

// Sessions
const sessions        = ref([])
const sessionsLoading = ref(false)

// Attempts (Hasil tab)
const attempts        = ref([])
const attemptsLoading = ref(false)
const grading         = reactive({}) // keyed by answerId: { points, feedback, saving, saved }

// Public link
const generatingCode  = ref(null)

// AI Generate
const showAiModal = ref(false)
const aiModel        = ref('Meta-Llama-3.3-70B-Instruct')
const aiProviderName = ref('DekaLLM')
const isAdmin     = ref(false)

// Import from question bank
const showImport        = ref(false)
const importCourses     = ref([])
const importCourseId    = ref(null)
const importBankItems   = ref([])
const importBankLoading = ref(false)
const importSelected    = ref(new Set())
const importTypeFilter  = ref('Semua')
const importing         = ref(false)

const importFiltered = computed(() => {
  if (importTypeFilter.value === 'Semua') return importBankItems.value
  const map = { 'PG': 'MultipleChoice', 'TF': 'TrueFalse', 'Essay': 'Essay' }
  return importBankItems.value.filter(q => q.type === map[importTypeFilter.value])
})

const importAllChecked = computed(() =>
  importFiltered.value.length > 0 && importFiltered.value.every(q => importSelected.value.has(q.id))
)

onMounted(async () => {
  await load()
  try {
    const aiStatus = await aiApi.status()
    isAdmin.value  = true
    aiModel.value        = aiStatus.data.model ?? 'Meta-Llama-3.3-70B-Instruct'
    aiProviderName.value = aiStatus.data.providerName ?? 'DekaLLM'
  } catch { /* bukan admin atau AI belum dikonfigurasi */ }
})

async function load() {
  loading.value = true
  try {
    const { data } = await mandatoryExamApi.getAll()
    exams.value = data
  } finally {
    loading.value = false
  }
}

async function createExam() {
  if (!form.title.trim()) return alert('Judul wajib diisi.')
  saving.value = true
  try {
    await mandatoryExamApi.create({ ...form })
    showCreate.value = false
    form.title = ''; form.description = ''; form.timeLimitMinutes = null; form.maxAttempts = 1; form.passScore = 60; form.questionsPerAttempt = null
    await load()
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal membuat ujian.')
  } finally {
    saving.value = false
  }
}

function openEdit(exam) {
  editTarget.value           = exam
  editForm.title             = exam.title
  editForm.description       = exam.description ?? ''
  editForm.timeLimitMinutes  = exam.timeLimitMinutes ?? null
  editForm.maxAttempts       = exam.maxAttempts
  editForm.passScore         = exam.passScore
  editForm.webhookUrl        = exam.webhookUrl ?? ''
  editForm.questionsPerAttempt = exam.questionsPerAttempt ?? null
  showEdit.value             = true
}

async function saveEdit() {
  if (!editForm.title.trim()) return alert('Judul wajib diisi.')
  editSaving.value = true
  try {
    await mandatoryExamApi.update(editTarget.value.id, { ...editForm })
    // Update data di list langsung tanpa reload
    const exam = exams.value.find(e => e.id === editTarget.value.id)
    if (exam) {
      exam.title            = editForm.title
      exam.description      = editForm.description
      exam.timeLimitMinutes = editForm.timeLimitMinutes
      exam.maxAttempts      = editForm.maxAttempts
      exam.passScore        = editForm.passScore
      exam.webhookUrl       = editForm.webhookUrl || null
      exam.questionsPerAttempt = editForm.questionsPerAttempt || null
    }
    // Update juga selected (detail drawer) jika sedang terbuka
    if (selected.value?.id === editTarget.value.id) {
      selected.value.title            = editForm.title
      selected.value.description      = editForm.description
      selected.value.timeLimitMinutes = editForm.timeLimitMinutes
      selected.value.maxAttempts      = editForm.maxAttempts
      selected.value.passScore        = editForm.passScore
      selected.value.webhookUrl       = editForm.webhookUrl || null
      selected.value.questionsPerAttempt = editForm.questionsPerAttempt || null
    }
    showEdit.value = false
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menyimpan perubahan.')
  } finally {
    editSaving.value = false
  }
}

async function toggleActive(exam) {
  try {
    await mandatoryExamApi.toggleActive(exam.id)
    exam.isActive = !exam.isActive
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mengubah status.')
  }
}

async function exportResults(exam) {
  try {
    const res = await mandatoryExamApi.exportResults(exam.id)
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = `hasil-ujian-${exam.title.replace(/\s+/g, '_')}.csv`
    a.click()
    URL.revokeObjectURL(url)
  } catch (e) {
    alert('Gagal export hasil ujian.')
  }
}

async function deleteExam(exam) {
  if (!confirm(`Hapus "${exam.title}"? Semua data akan ikut terhapus.`)) return
  try {
    await mandatoryExamApi.delete(exam.id)
    exams.value = exams.value.filter(e => e.id !== exam.id)
    if (selected.value?.id === exam.id) selected.value = null
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus.')
  }
}

async function openDetail(exam) {
  selected.value      = exam
  activeTab.value     = 'Soal'
  detailLoading.value = true
  detail.value        = null
  generatedLink.value = null
  sessions.value      = []
  attempts.value      = []
  try {
    const { data } = await mandatoryExamApi.getById(exam.id)
    detail.value = data
  } finally {
    detailLoading.value = false
  }
}

async function loadSessions() {
  if (!selected.value) return
  sessionsLoading.value = true
  try {
    const { data } = await mandatoryExamApi.getSessions(selected.value.id)
    sessions.value = data
  } finally {
    sessionsLoading.value = false
  }
}

async function revokeSession(s) {
  if (!confirm('Cabut link ini? User tidak akan bisa menggunakan link tersebut lagi.')) return
  try {
    await mandatoryExamApi.revokeSession(s.id)
    s.isRevoked = true
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mencabut link.')
  }
}

function onQTypeChange() {
  if (qForm.type === 'TrueFalse') {
    qForm.options = [{ text: 'Benar', isCorrect: true }, { text: 'Salah', isCorrect: false }]
  } else if (qForm.type === 'MultipleChoice') {
    qForm.options = [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
  } else {
    qForm.options = []
  }
}

async function addQuestion() {
  if (!qForm.text.trim()) return alert('Pertanyaan wajib diisi.')
  savingQ.value = true
  try {
    await mandatoryExamApi.addQuestion(selected.value.id, { ...qForm })
    showAddQ.value = false
    qForm.text = ''; qForm.type = 'MultipleChoice'; qForm.points = 10
    qForm.options = [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
    const { data } = await mandatoryExamApi.getById(selected.value.id)
    detail.value = data
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menyimpan soal.')
  } finally {
    savingQ.value = false
  }
}

function openEditQ(q) {
  editingQId.value = q.id
  editQForm.text   = q.text
  editQForm.points = q.points
  editQForm.options = (q.options ?? []).map(o => ({ text: o.text, isCorrect: o.isCorrect }))
}

async function saveEditQ(q) {
  if (!editQForm.text.trim()) return alert('Teks soal tidak boleh kosong.')
  savingQEdit.value = true
  try {
    const { data } = await mandatoryExamApi.updateQuestion(selected.value.id, q.id, {
      text: editQForm.text,
      points: editQForm.points,
      options: editQForm.options,
    })
    // Update in place
    const idx = detail.value.questions.findIndex(x => x.id === q.id)
    if (idx !== -1) detail.value.questions[idx] = data
    editingQId.value = null
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menyimpan soal.')
  } finally {
    savingQEdit.value = false
  }
}

async function moveQuestion(idx, dir) {
  const qs  = detail.value.questions
  const newIdx = idx + dir
  if (newIdx < 0 || newIdx >= qs.length) return

  // Swap locally
  const temp   = qs[idx]
  qs[idx]      = qs[newIdx]
  qs[newIdx]   = temp

  // Reassign order values
  qs.forEach((q, i) => { q.order = i })

  reordering.value = true
  try {
    await mandatoryExamApi.reorderQuestions(
      selected.value.id,
      qs.map((q, i) => ({ questionId: q.id, order: i }))
    )
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menyimpan urutan.')
    // Rollback swap
    const temp2   = qs[idx]
    qs[idx]       = qs[newIdx]
    qs[newIdx]    = temp2
    qs.forEach((q, i) => { q.order = i })
  } finally {
    reordering.value = false
  }
}

async function deleteQuestion(qId) {
  if (!confirm('Hapus soal ini?')) return
  try {
    await mandatoryExamApi.deleteQuestion(selected.value.id, qId)
    detail.value.questions = detail.value.questions.filter(q => q.id !== qId)
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus soal.')
  }
}

async function assignUser() {
  if (!assignUserId.value.trim() || !assignUserName.value.trim()) return alert('User ID dan nama wajib diisi.')
  assigning.value = true
  try {
    await mandatoryExamApi.assign(selected.value.id, { userId: assignUserId.value, userName: assignUserName.value })
    assignUserId.value = ''; assignUserName.value = ''
    const { data } = await mandatoryExamApi.getById(selected.value.id)
    detail.value = data
    await load()
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal assign user.')
  } finally {
    assigning.value = false
  }
}

async function unassignUser(userId) {
  if (!confirm('Hapus user dari ujian ini?')) return
  try {
    await mandatoryExamApi.unassign(selected.value.id, userId)
    detail.value.assignments = detail.value.assignments.filter(a => a.userId !== userId)
    await load()
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus user.')
  }
}

async function generateLink() {
  generatingLink.value = true
  try {
    const { data } = await mandatoryExamApi.generateLink(selected.value.id, {
      expiryMinutes: linkExpiry.value
    })
    generatedLink.value = data
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal generate link.')
  } finally {
    generatingLink.value = false
  }
}

function copyLink() {
  navigator.clipboard.writeText(generatedLink.value.deepLink)
    .then(() => alert('Link disalin!'))
}

function copyExamId(id) {
  navigator.clipboard.writeText(String(id))
    .then(() => alert(`Exam ID ${id} disalin!`))
}

async function loadAttempts() {
  if (!selected.value) return
  attemptsLoading.value = true
  try {
    const { data } = await mandatoryExamApi.getAttempts(selected.value.id)
    attempts.value = data
    // Init grading state for each essay answer
    for (const att of data) {
      for (const ea of att.essayAnswers ?? []) {
        if (!grading[ea.answerId]) {
          grading[ea.answerId] = {
            points:   ea.earnedPoints ?? 0,
            feedback: ea.feedback ?? '',
            saving:   false,
            saved:    false,
          }
        }
      }
    }
  } finally {
    attemptsLoading.value = false
  }
}

async function saveGrade(att, essayAnswer) {
  const g = grading[essayAnswer.answerId]
  g.saving = true
  g.saved  = false
  try {
    const { data } = await mandatoryExamApi.gradeEssay(att.id, essayAnswer.answerId, {
      earnedPoints: g.points,
      feedback:     g.feedback || null,
    })
    // Update attempt score/percentage/isPassed in place
    att.score      = data.score
    att.percentage = data.percentage
    att.isPassed   = data.isPassed
    essayAnswer.earnedPoints = g.points
    essayAnswer.feedback     = g.feedback
    g.saved = true
    setTimeout(() => { g.saved = false }, 3000)
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menyimpan nilai.')
  } finally {
    g.saving = false
  }
}

async function openImportModal() {
  showImport.value      = true
  importCourseId.value  = null
  importBankItems.value = []
  importSelected.value  = new Set()
  importTypeFilter.value = 'Semua'
  if (importCourses.value.length === 0) {
    try {
      const { data } = await coursesApi.getMy()
      importCourses.value = Array.isArray(data) ? data : (data.items ?? data.courses ?? [])
    } catch { importCourses.value = [] }
  }
}

async function onImportCourseChange() {
  if (!importCourseId.value) return
  importBankLoading.value = true
  importBankItems.value   = []
  importSelected.value    = new Set()
  try {
    const { data } = await courseQuestionBankApi.getAll(importCourseId.value)
    importBankItems.value = data
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal memuat bank soal.')
  } finally {
    importBankLoading.value = false
  }
}

function setImportTypeFilter(f) {
  importTypeFilter.value = f
  importSelected.value   = new Set()
}

function toggleImportSelect(id) {
  const s = new Set(importSelected.value)
  s.has(id) ? s.delete(id) : s.add(id)
  importSelected.value = s
}

function toggleImportAll() {
  if (importAllChecked.value) {
    const s = new Set(importSelected.value)
    importFiltered.value.forEach(q => s.delete(q.id))
    importSelected.value = s
  } else {
    const s = new Set(importSelected.value)
    importFiltered.value.forEach(q => s.add(q.id))
    importSelected.value = s
  }
}

async function doImport() {
  if (importSelected.value.size === 0) return alert('Pilih minimal satu soal.')
  importing.value = true
  try {
    const { data } = await mandatoryExamApi.importQuestions(selected.value.id, {
      questionBankIds: [...importSelected.value]
    })
    showImport.value = false
    // Reload soal list
    const { data: detail2 } = await mandatoryExamApi.getById(selected.value.id)
    detail.value = detail2
    // Update question count on exam card
    const examInList = exams.value.find(e => e.id === selected.value.id)
    if (examInList) examInList.questionCount = detail2.questions.length
    alert(data.message)
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mengimpor soal.')
  } finally {
    importing.value = false
  }
}

async function revokePublicLink(exam) {
  if (!confirm(`Cabut link publik "${exam.title}"?\nSemua user yang belum submit tidak akan bisa mengakses exam via link ini. Anda bisa generate link baru setelahnya.`)) return
  try {
    await mandatoryExamApi.revokePublicLink(exam.id)
    exam.publicAccessCode = null
    exam.publicLink       = null
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mencabut link.')
  }
}

async function generatePublicLink(exam) {
  if (!confirm(`Generate link publik untuk "${exam.title}"?\nLink ini bisa diakses siapapun yang punya URL-nya.`)) return
  generatingCode.value = exam.id
  try {
    const { data } = await mandatoryExamApi.generateAccessCode(exam.id)
    exam.publicAccessCode = data.code
    exam.publicLink = data.publicLink
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal generate link.')
  } finally {
    generatingCode.value = null
  }
}

function copyPublicLink(link) {
  const fullLink = `${link}&userId={USER_ID}&userName={USER_NAME}`
  navigator.clipboard.writeText(fullLink)
    .then(() => alert('Link disalin! Ganti {USER_ID} dan {USER_NAME} dengan data user DWI Mobile.'))
}

async function onAiSave(generatedQuestions) {
  for (const q of generatedQuestions) {
    try {
      await mandatoryExamApi.addQuestion(selected.value.id, {
        text:    q.text,
        type:    q.type,
        points:  q.points,
        options: (q.options ?? []).map(o => ({ text: o.text, isCorrect: o.isCorrect })),
      })
    } catch (e) {
      console.error('Gagal simpan soal AI:', e)
    }
  }
  showAiModal.value = false
  // Reload soal list
  const { data } = await mandatoryExamApi.getById(selected.value.id)
  detail.value = data
  const examInList = exams.value.find(e => e.id === selected.value.id)
  if (examInList) examInList.questionCount = data.questions.length
}

async function exportCSV() {
  try {
    const res = await mandatoryExamApi.exportResults(selected.value.id)
    const url = URL.createObjectURL(new Blob([res.data], { type: 'text/csv' }))
    const a   = document.createElement('a')
    a.href     = url
    a.download = `hasil-ujian-${selected.value.title}.csv`
    a.click()
    URL.revokeObjectURL(url)
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mengunduh CSV.')
  }
}

function formatDate(d) {
  return new Date(d).toLocaleString('id-ID', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>
