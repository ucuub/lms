using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Data;

/// <summary>
/// Mengisi database dengan data contoh mirip Moodle.
/// Hanya berjalan jika belum ada course di database.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(LmsDbContext db)
    {
        if (await db.Courses.AnyAsync()) return;

        // ── Seed Users ────────────────────────────────────────────────────────
        // Gunakan user yang sudah login via Keycloak, atau buat seed user
        var teacher = await db.AppUsers.FirstOrDefaultAsync(u => u.Role == "teacher")
            ?? await CreateUser(db, "seed-teacher-1", "Dewi Lestari", "teacher1@lms.local", "teacher");

        var teacher2 = await db.AppUsers.FirstOrDefaultAsync(u => u.Role == "teacher" && u.UserId != teacher.UserId)
            ?? await CreateUser(db, "seed-teacher-2", "Bima Prakoso", "teacher2@lms.local", "teacher");

        var student1 = await db.AppUsers.FirstOrDefaultAsync(u => u.Role == "student")
            ?? await CreateUser(db, "seed-student-1", "Budi Santoso", "student1@lms.local", "student");

        var student2 = await db.AppUsers.FirstOrDefaultAsync(u => u.Role == "student" && u.UserId != student1.UserId)
            ?? await CreateUser(db, "seed-student-2", "Sari Indah", "student2@lms.local", "student");

        var student3 = await CreateUser(db, "seed-student-3", "Rizky Pratama", "student3@lms.local", "student");
        var student4 = await CreateUser(db, "seed-student-4", "Nurul Hidayah", "student4@lms.local", "student");

        // ── Course 1: Pemrograman Web ─────────────────────────────────────────
        var course1 = new Course
        {
            Title          = "Pemrograman Web Dasar: HTML, CSS & JavaScript",
            Description    = "Pelajari fondasi pemrograman web dari nol. Kursus ini dirancang untuk pemula yang ingin memulai karir sebagai web developer. Kamu akan belajar membuat halaman web yang menarik dan interaktif.",
            Category       = "Pemrograman",
            Level          = "Pemula",
            InstructorId   = teacher.UserId,
            InstructorName = teacher.Name,
            IsPublished    = true,
        };
        db.Courses.Add(course1);
        await db.SaveChangesAsync();

        // Modules Course 1
        var modules1 = new List<CourseModule>
        {
            new() {
                CourseId = course1.Id, Order = 0, IsPublished = true,
                Title       = "Pengenalan HTML: Struktur Halaman Web",
                ContentType = ModuleContentType.Text,
                Content = @"<h2>Apa itu HTML?</h2>
<p>HTML (<strong>HyperText Markup Language</strong>) adalah bahasa markup standar untuk membuat halaman web. HTML mendeskripsikan struktur halaman web menggunakan elemen-elemen yang direpresentasikan oleh tag.</p>

<h3>Struktur Dasar HTML</h3>
<pre><code>&lt;!DOCTYPE html&gt;
&lt;html lang=""id""&gt;
&lt;head&gt;
    &lt;meta charset=""UTF-8""&gt;
    &lt;title&gt;Halaman Pertamaku&lt;/title&gt;
&lt;/head&gt;
&lt;body&gt;
    &lt;h1&gt;Halo Dunia!&lt;/h1&gt;
    &lt;p&gt;Ini halaman web pertamaku.&lt;/p&gt;
&lt;/body&gt;
&lt;/html&gt;</code></pre>

<h3>Tag HTML yang Penting</h3>
<ul>
    <li><code>&lt;h1&gt; - &lt;h6&gt;</code> : Heading / Judul</li>
    <li><code>&lt;p&gt;</code> : Paragraf</li>
    <li><code>&lt;a&gt;</code> : Link / Tautan</li>
    <li><code>&lt;img&gt;</code> : Gambar</li>
    <li><code>&lt;div&gt;</code> : Kontainer blok</li>
    <li><code>&lt;span&gt;</code> : Kontainer inline</li>
    <li><code>&lt;ul&gt;, &lt;ol&gt;, &lt;li&gt;</code> : Daftar</li>
    <li><code>&lt;table&gt;</code> : Tabel</li>
    <li><code>&lt;form&gt;</code> : Formulir</li>
</ul>

<h3>Atribut HTML</h3>
<p>Atribut memberikan informasi tambahan pada elemen HTML:</p>
<pre><code>&lt;a href=""https://www.google.com"" target=""_blank""&gt;Kunjungi Google&lt;/a&gt;
&lt;img src=""foto.jpg"" alt=""Foto saya"" width=""300""&gt;</code></pre>

<blockquote>
    <strong>Tips:</strong> Selalu gunakan atribut <code>alt</code> pada gambar untuk aksesibilitas dan SEO.
</blockquote>"
            },
            new() {
                CourseId = course1.Id, Order = 1, IsPublished = true,
                Title       = "CSS: Menghias Halaman Web",
                ContentType = ModuleContentType.Mixed,
                VideoUrl      = "https://www.youtube.com/watch?v=1Rs2ND1ryYc",
                VideoEmbedId  = "1Rs2ND1ryYc",
                VideoProvider = VideoProvider.YouTube,
                Content = @"<h2>Apa itu CSS?</h2>
<p>CSS (<strong>Cascading Style Sheets</strong>) digunakan untuk mendeskripsikan tampilan dan format dokumen HTML. Dengan CSS, kita bisa mengontrol warna, font, tata letak, dan banyak aspek visual lainnya.</p>

<h3>Cara Menambahkan CSS</h3>
<p>Ada 3 cara menambahkan CSS ke HTML:</p>
<ol>
    <li><strong>Inline CSS</strong> - langsung di element: <code>&lt;p style=""color: red;""&gt;</code></li>
    <li><strong>Internal CSS</strong> - di dalam tag <code>&lt;style&gt;</code></li>
    <li><strong>External CSS</strong> - file terpisah <code>style.css</code> (rekomendasi)</li>
</ol>

<h3>Selector CSS</h3>
<pre><code>/* Selector element */
p { color: #333; }

/* Selector class */
.highlight { background-color: yellow; }

/* Selector ID */
#header { font-size: 2rem; }

/* Selector kombinasi */
.card h2 { margin-bottom: 10px; }</code></pre>

<h3>Box Model CSS</h3>
<p>Setiap elemen HTML adalah sebuah kotak yang terdiri dari:</p>
<ul>
    <li><strong>Content</strong> - isi konten</li>
    <li><strong>Padding</strong> - jarak antara konten dan border</li>
    <li><strong>Border</strong> - garis pembatas</li>
    <li><strong>Margin</strong> - jarak luar dari elemen</li>
</ul>

<h3>Flexbox - Layout Modern</h3>
<pre><code>.container {
    display: flex;
    justify-content: center;  /* horizontal */
    align-items: center;      /* vertical */
    gap: 16px;
}</code></pre>"
            },
            new() {
                CourseId = course1.Id, Order = 2, IsPublished = true,
                Title       = "JavaScript: Membuat Web Interaktif",
                ContentType = ModuleContentType.Text,
                Content = @"<h2>JavaScript: Bahasa Pemrograman Web</h2>
<p>JavaScript adalah bahasa pemrograman yang membuat halaman web menjadi interaktif dan dinamis. Hampir semua website modern menggunakan JavaScript.</p>

<h3>Dasar-dasar JavaScript</h3>
<pre><code>// Variabel
let nama = 'Budi';
const umur = 25;

// Fungsi
function sapa(nama) {
    return `Halo, ${nama}!`;
}

// Arrow function (ES6)
const tambah = (a, b) => a + b;

// Array
const buah = ['apel', 'mangga', 'jeruk'];
buah.push('pisang');

// Object
const mahasiswa = {
    nama: 'Budi',
    nim: '12345',
    nilai: 90
};</code></pre>

<h3>Manipulasi DOM</h3>
<pre><code>// Ambil element
const tombol = document.getElementById('btn');
const judul = document.querySelector('h1');

// Ubah konten
judul.textContent = 'Judul Baru';
judul.innerHTML = '&lt;span&gt;Judul Baru&lt;/span&gt;';

// Event listener
tombol.addEventListener('click', function() {
    alert('Tombol diklik!');
});

// Tambah/hapus class
judul.classList.add('aktif');
judul.classList.remove('aktif');
judul.classList.toggle('aktif');</code></pre>

<h3>Fetch API - Ambil Data dari Server</h3>
<pre><code>async function ambilData() {
    try {
        const response = await fetch('https://api.example.com/data');
        const data = await response.json();
        console.log(data);
    } catch (error) {
        console.error('Error:', error);
    }
}</code></pre>"
            },
            new() {
                CourseId = course1.Id, Order = 3, IsPublished = true,
                Title       = "Responsive Design dengan CSS Grid",
                ContentType = ModuleContentType.Text,
                Content = @"<h2>Responsive Web Design</h2>
<p>Responsive design memastikan website terlihat bagus di semua ukuran layar, dari smartphone hingga desktop.</p>

<h3>Viewport Meta Tag</h3>
<pre><code>&lt;meta name=""viewport"" content=""width=device-width, initial-scale=1.0""&gt;</code></pre>

<h3>Media Queries</h3>
<pre><code>/* Mobile first */
.container { padding: 16px; }

/* Tablet */
@media (min-width: 768px) {
    .container { padding: 24px; }
}

/* Desktop */
@media (min-width: 1024px) {
    .container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 32px;
    }
}</code></pre>

<h3>CSS Grid Layout</h3>
<pre><code>.grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 24px;
}

/* Grid 3 kolom di desktop, 1 kolom di mobile */
.card-grid {
    display: grid;
    grid-template-columns: 1fr;
    gap: 16px;
}

@media (min-width: 768px) {
    .card-grid {
        grid-template-columns: repeat(3, 1fr);
    }
}</code></pre>"
            },
            new() {
                CourseId = course1.Id, Order = 4, IsPublished = true,
                Title       = "Project: Membuat Website Portfolio",
                ContentType = ModuleContentType.Text,
                Content = @"<h2>Project Akhir: Website Portfolio</h2>
<p>Pada modul ini kita akan membuat website portfolio lengkap menggunakan HTML, CSS, dan JavaScript yang telah dipelajari.</p>

<h3>Struktur Project</h3>
<pre><code>portfolio/
├── index.html
├── css/
│   └── style.css
├── js/
│   └── main.js
└── img/
    └── foto.jpg</code></pre>

<h3>Bagian-bagian Portfolio</h3>
<ol>
    <li><strong>Header/Hero</strong> - foto, nama, dan tagline</li>
    <li><strong>About Me</strong> - deskripsi singkat tentang diri</li>
    <li><strong>Skills</strong> - kemampuan teknis yang dikuasai</li>
    <li><strong>Projects</strong> - showcase project yang pernah dibuat</li>
    <li><strong>Contact</strong> - form kontak dan social media</li>
</ol>

<h3>Tips Membuat Portfolio yang Baik</h3>
<ul>
    <li>Gunakan warna yang konsisten (pilih 2-3 warna utama)</li>
    <li>Pastikan responsive di mobile</li>
    <li>Loading cepat (optimalkan gambar)</li>
    <li>Tampilkan minimal 3 project terbaik</li>
    <li>Sertakan link ke GitHub dan LinkedIn</li>
</ul>

<blockquote>
    <strong>Tugas:</strong> Buat website portfolio kamu sendiri dan upload ke GitHub Pages. Link akan dikumpulkan di assignment berikutnya.
</blockquote>"
            },
        };
        db.CourseModules.AddRange(modules1);
        await db.SaveChangesAsync();

        // Assignments Course 1
        var due1 = DateTime.UtcNow.AddDays(7);
        var due2 = DateTime.UtcNow.AddDays(14);
        var assignments1 = new List<Assignment>
        {
            new() {
                CourseId    = course1.Id,
                Title       = "Buat Halaman Profil dengan HTML",
                Description = @"Buatlah halaman profil diri kamu menggunakan HTML dengan ketentuan berikut:

1. Gunakan struktur HTML yang benar (DOCTYPE, html, head, body)
2. Tampilkan: nama lengkap, foto (boleh placeholder), jurusan/pekerjaan, deskripsi singkat
3. Sertakan daftar minimal 3 keahlian/hobi menggunakan tag list
4. Tambahkan link ke media sosial (minimal 1)
5. Gunakan heading, paragraf, dan tag lainnya yang sesuai

**Format pengumpulan:** Upload file .html

**Kriteria penilaian:**
- Struktur HTML valid (30 poin)
- Kelengkapan konten (40 poin)
- Kreativitas layout (30 poin)",
                DueDate  = due1,
                MaxScore = 100,
            },
            new() {
                CourseId    = course1.Id,
                Title       = "Portfolio Website dengan CSS & JavaScript",
                Description = @"Kembangkan halaman profil dari assignment sebelumnya menjadi website portfolio lengkap dengan CSS dan JavaScript.

**Requirement:**
1. Styling CSS yang rapi dan konsisten
2. Responsive design (mobile & desktop)
3. Minimal 1 animasi atau efek interaktif dengan JavaScript
4. Navigasi yang berfungsi (smooth scroll)
5. Form kontak (tidak harus fungsional)

**Bonus:**
- Deploy ke GitHub Pages (+10 poin)
- Animasi CSS yang kreatif (+5 poin)

**Format pengumpulan:** Link GitHub repository atau URL website live",
                DueDate  = due2,
                MaxScore = 100,
            },
        };
        db.Assignments.AddRange(assignments1);
        await db.SaveChangesAsync();

        // Quiz Course 1
        var quiz1 = new Quiz
        {
            CourseId       = course1.Id,
            Title          = "Quiz: Dasar-dasar HTML & CSS",
            Description    = "Uji pemahaman kamu tentang HTML dan CSS yang telah dipelajari pada modul 1-3.",
            TimeLimitMinutes = 20,
            MaxAttempts    = 2,
            PassScore      = 70,
            IsPublished    = true,
        };
        db.Quizzes.Add(quiz1);
        await db.SaveChangesAsync();

        var questions1 = new List<Question>
        {
            new() {
                QuizId = quiz1.Id, Order = 0, Points = 20,
                Text = "Tag HTML yang digunakan untuk membuat paragraf adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "<p>", IsCorrect = true },
                    new() { Text = "<paragraph>", IsCorrect = false },
                    new() { Text = "<text>", IsCorrect = false },
                    new() { Text = "<span>", IsCorrect = false },
                ]
            },
            new() {
                QuizId = quiz1.Id, Order = 1, Points = 20,
                Text = "Property CSS yang digunakan untuk mengubah warna teks adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "text-color", IsCorrect = false },
                    new() { Text = "font-color", IsCorrect = false },
                    new() { Text = "color", IsCorrect = true },
                    new() { Text = "foreground-color", IsCorrect = false },
                ]
            },
            new() {
                QuizId = quiz1.Id, Order = 2, Points = 20,
                Text = "Flexbox adalah fitur CSS yang digunakan untuk...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "Mengubah warna elemen", IsCorrect = false },
                    new() { Text = "Mengatur tata letak (layout) elemen secara fleksibel", IsCorrect = true },
                    new() { Text = "Menambahkan animasi", IsCorrect = false },
                    new() { Text = "Membuat gambar responsif", IsCorrect = false },
                ]
            },
            new() {
                QuizId = quiz1.Id, Order = 3, Points = 20,
                Text = "Atribut HTML yang wajib ada pada tag <img> untuk aksesibilitas adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "src", IsCorrect = false },
                    new() { Text = "width", IsCorrect = false },
                    new() { Text = "alt", IsCorrect = true },
                    new() { Text = "title", IsCorrect = false },
                ]
            },
            new() {
                QuizId = quiz1.Id, Order = 4, Points = 20,
                Text = "CSS selector yang benar untuk menargetkan elemen dengan class 'btn-primary' adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "#btn-primary", IsCorrect = false },
                    new() { Text = ".btn-primary", IsCorrect = true },
                    new() { Text = "*btn-primary", IsCorrect = false },
                    new() { Text = "@btn-primary", IsCorrect = false },
                ]
            },
        };
        db.Questions.AddRange(questions1);
        await db.SaveChangesAsync();

        // Announcements Course 1
        db.Announcements.AddRange(
            new Announcement {
                CourseId   = course1.Id,
                Title      = "Selamat Datang di Kursus Pemrograman Web!",
                Content    = "Halo semua! Selamat datang di kursus Pemrograman Web Dasar. Saya sangat senang bisa mengajar kalian. Kursus ini akan berlangsung selama 4 minggu dengan total 5 modul materi, 2 assignment, dan 1 quiz. Jangan ragu untuk bertanya di forum diskusi jika ada yang tidak dimengerti. Semangat belajar! 🚀",
                AuthorId   = teacher.UserId,
                AuthorName = teacher.Name,
                IsPinned   = true,
            },
            new Announcement {
                CourseId   = course1.Id,
                Title      = "Pengingat Deadline Assignment 1",
                Content    = "Mengingatkan bahwa deadline Assignment 1 (Buat Halaman Profil dengan HTML) adalah akhir minggu ini. Pastikan kalian mengumpulkan sebelum deadline. Jika ada pertanyaan tentang assignment, tanyakan di forum atau hubungi saya.",
                AuthorId   = teacher.UserId,
                AuthorName = teacher.Name,
                IsPinned   = false,
            }
        );

        // Forum Course 1
        db.ForumPosts.AddRange(
            new ForumPost {
                CourseId  = course1.Id,
                UserId    = student1.UserId,
                UserName  = student1.Name,
                Title     = "Perbedaan margin dan padding di CSS?",
                Body      = "Halo kak, saya masih bingung dengan perbedaan margin dan padding di CSS. Kapan harus menggunakan margin dan kapan padding? Terima kasih",
                IsPinned  = false,
            },
            new ForumPost {
                CourseId  = course1.Id,
                UserId    = student2.UserId,
                UserName  = student2.Name,
                Title     = "Cara membuat navbar responsive?",
                Body      = "Saya sudah mencoba membuat navbar tapi di mobile tampilannya berantakan. Ada yang bisa bantu? Saya sudah coba pakai media query tapi masih bingung.",
                IsPinned  = false,
            }
        );

        // Reviews Course 1
        db.CourseReviews.AddRange(
            new CourseReview {
                CourseId = course1.Id, UserId = student1.UserId,
                UserName = student1.Name, Rating = 5,
                Comment  = "Materi sangat lengkap dan mudah dipahami. Penjelasan step-by-step sangat membantu pemula seperti saya. Highly recommended!",
            },
            new CourseReview {
                CourseId = course1.Id, UserId = student2.UserId,
                UserName = student2.Name, Rating = 4,
                Comment  = "Kursus bagus, materi cukup lengkap. Saran: tambahkan lebih banyak contoh praktis dan video tutorial.",
            }
        );

        // Enrollments Course 1
        db.Enrollments.AddRange(
            new Enrollment { CourseId = course1.Id, UserId = student1.UserId, UserName = student1.Name, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = course1.Id, UserId = student2.UserId, UserName = student2.Name, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = course1.Id, UserId = student3.UserId, UserName = student3.Name, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = course1.Id, UserId = student4.UserId, UserName = student4.Name, Status = EnrollmentStatus.Completed, CompletedAt = DateTime.UtcNow.AddDays(-5) }
        );

        // Progress student1 (sudah selesaikan semua modul → bisa klaim sertifikat)
        var progress1 = new CourseProgress {
            CourseId = course1.Id, UserId = student1.UserId,
            CompletedModules = 5, TotalModules = 5,
            LastAccessedAt = DateTime.UtcNow.AddHours(-2),
            CompletedAt = DateTime.UtcNow.AddDays(-1),
        };
        db.CourseProgresses.Add(progress1);
        await db.SaveChangesAsync();

        db.ModuleProgresses.AddRange(
            new ModuleProgress { CourseProgressId = progress1.Id, ModuleId = modules1[0].Id, UserId = student1.UserId },
            new ModuleProgress { CourseProgressId = progress1.Id, ModuleId = modules1[1].Id, UserId = student1.UserId },
            new ModuleProgress { CourseProgressId = progress1.Id, ModuleId = modules1[2].Id, UserId = student1.UserId },
            new ModuleProgress { CourseProgressId = progress1.Id, ModuleId = modules1[3].Id, UserId = student1.UserId },
            new ModuleProgress { CourseProgressId = progress1.Id, ModuleId = modules1[4].Id, UserId = student1.UserId }
        );

        // Progress student4 (sudah completed — sinkron dengan enrollment.CompletedAt & certificate)
        var progress4 = new CourseProgress {
            CourseId = course1.Id, UserId = student4.UserId,
            CompletedModules = 5, TotalModules = 5,
            LastAccessedAt = DateTime.UtcNow.AddDays(-5),
            CompletedAt    = DateTime.UtcNow.AddDays(-5),
        };
        db.CourseProgresses.Add(progress4);
        await db.SaveChangesAsync();

        db.ModuleProgresses.AddRange(
            new ModuleProgress { CourseProgressId = progress4.Id, ModuleId = modules1[0].Id, UserId = student4.UserId },
            new ModuleProgress { CourseProgressId = progress4.Id, ModuleId = modules1[1].Id, UserId = student4.UserId },
            new ModuleProgress { CourseProgressId = progress4.Id, ModuleId = modules1[2].Id, UserId = student4.UserId },
            new ModuleProgress { CourseProgressId = progress4.Id, ModuleId = modules1[3].Id, UserId = student4.UserId },
            new ModuleProgress { CourseProgressId = progress4.Id, ModuleId = modules1[4].Id, UserId = student4.UserId }
        );

        // Submission + Grade student1 assignment 1
        db.Submissions.Add(new Submission {
            AssignmentId = assignments1[0].Id,
            UserId       = student1.UserId,
            UserName     = student1.Name,
            TextContent  = "Saya telah membuat halaman profil dengan menggunakan HTML. File terlampir.",
            FileName     = "profil-budi.html",
            FileUrl      = "/uploads/submissions/profil-budi.html",
            Status       = SubmissionStatus.Graded,
            Score        = 88,
            Feedback     = "Bagus! Struktur HTML sudah benar dan konten lengkap. Untuk nilai lebih tinggi, coba tambahkan lebih banyak semantic HTML seperti <header>, <main>, <footer>.",
            SubmittedAt  = DateTime.UtcNow.AddDays(-3),
            GradedAt     = DateTime.UtcNow.AddDays(-1),
        });

        // Certificate student1 (Budi — bisa dilihat via MockUserSwitcher)
        db.Certificates.Add(new Certificate {
            CourseId          = course1.Id,
            UserId            = student1.UserId,
            UserName          = student1.Name,
            CertificateNumber = $"LMS-{DateTime.UtcNow.Year}-WEB-0001",
            IssuedAt          = DateTime.UtcNow.AddDays(-1),
        });

        // Certificate student4 (sudah completed — bisa dilihat via "Nurul (Sertifikat)" di switcher)
        db.Certificates.Add(new Certificate {
            CourseId          = course1.Id,
            UserId            = student4.UserId,
            UserName          = student4.Name,
            CertificateNumber = $"LMS-{DateTime.UtcNow.Year}-WEB-0002",
            IssuedAt          = DateTime.UtcNow.AddDays(-5),
        });

        await db.SaveChangesAsync();

        // ── Course 2: UI/UX Design ────────────────────────────────────────────
        var course2 = new Course
        {
            Title          = "UI/UX Design dengan Figma: Dari Wireframe ke Prototype",
            Description    = "Kuasai dunia desain digital dengan Figma. Belajar prinsip-prinsip desain UI/UX modern, membuat wireframe, mockup, dan prototype interaktif yang siap dipresentasikan ke klien atau tim developer.",
            Category       = "Desain",
            Level          = "Menengah",
            InstructorId   = teacher2.UserId,
            InstructorName = teacher2.Name,
            IsPublished    = true,
        };
        db.Courses.Add(course2);
        await db.SaveChangesAsync();

        var modules2 = new List<CourseModule>
        {
            new() {
                CourseId = course2.Id, Order = 0, IsPublished = true,
                Title = "Pengenalan UI/UX Design",
                Content = @"<h2>Apa itu UI/UX Design?</h2>
<p><strong>UI (User Interface)</strong> adalah tampilan visual dari sebuah produk digital — semua yang dilihat dan diklik pengguna.</p>
<p><strong>UX (User Experience)</strong> adalah keseluruhan pengalaman pengguna saat berinteraksi dengan produk — apakah mudah digunakan, menyenangkan, dan efisien.</p>

<h3>Perbedaan UI dan UX</h3>
<table>
    <tr><th>UI Design</th><th>UX Design</th></tr>
    <tr><td>Tampilan visual</td><td>Alur pengguna</td></tr>
    <tr><td>Warna, tipografi, ikon</td><td>Riset, wireframe, testing</td></tr>
    <tr><td>Estetika</td><td>Fungsionalitas</td></tr>
</table>

<h3>Proses Desain UX (Design Thinking)</h3>
<ol>
    <li><strong>Empathize</strong> - Pahami pengguna: siapa mereka, apa masalah mereka?</li>
    <li><strong>Define</strong> - Definisikan masalah yang akan diselesaikan</li>
    <li><strong>Ideate</strong> - Brainstorming solusi kreatif</li>
    <li><strong>Prototype</strong> - Buat prototype cepat untuk diuji</li>
    <li><strong>Test</strong> - Uji prototype dengan pengguna nyata</li>
</ol>

<h3>Tools UI/UX Designer</h3>
<ul>
    <li><strong>Figma</strong> - Desain kolaboratif (kita akan pakai ini)</li>
    <li><strong>Adobe XD</strong> - Alternatif dari Adobe</li>
    <li><strong>Sketch</strong> - Populer di ekosistem Mac</li>
    <li><strong>InVision</strong> - Prototyping dan feedback</li>
</ul>"
            },
            new() {
                CourseId = course2.Id, Order = 1, IsPublished = true,
                Title = "Figma: Fitur Dasar dan Interface",
                VideoUrl = "https://www.youtube.com/watch?v=FTFaQWZBqQ8",
                VideoEmbedId = "FTFaQWZBqQ8", VideoProvider = VideoProvider.YouTube,
                Content = @"<h2>Mengenal Interface Figma</h2>
<p>Figma adalah tool desain berbasis browser yang bisa diakses gratis di <a href='https://figma.com'>figma.com</a>.</p>

<h3>Panel-panel Utama Figma</h3>
<ul>
    <li><strong>Canvas</strong> - Area kerja utama untuk mendesain</li>
    <li><strong>Layers Panel</strong> - Daftar semua layer/objek (kiri)</li>
    <li><strong>Properties Panel</strong> - Properti objek yang dipilih (kanan)</li>
    <li><strong>Toolbar</strong> - Tools seperti Select, Frame, Shape, Text (atas)</li>
</ul>

<h3>Shortcut Penting Figma</h3>
<pre><code>F       - Buat Frame baru
R       - Rectangle
T       - Text
V       - Select tool
Ctrl+D  - Duplicate
Ctrl+G  - Group
Ctrl+Z  - Undo
Space   - Pan/geser canvas
Ctrl+/  - Quick actions</code></pre>

<h3>Auto Layout</h3>
<p>Auto Layout adalah fitur Figma untuk membuat elemen yang otomatis menyesuaikan ukuran kontennya — sangat berguna untuk membuat komponen yang responsif.</p>"
            },
            new() {
                CourseId = course2.Id, Order = 2, IsPublished = true,
                Title = "Prinsip Desain: Typography & Color Theory",
                Content = @"<h2>Typography dalam UI Design</h2>
<p>Typography adalah seni mengatur teks agar mudah dibaca dan menarik secara visual.</p>

<h3>Hierarki Typography</h3>
<pre><code>H1: 32-48px (Page title)
H2: 24-32px (Section title)
H3: 20-24px (Card title)
Body: 14-16px (Konten)
Caption: 12px (Keterangan kecil)</code></pre>

<h3>Font yang Direkomendasikan</h3>
<ul>
    <li><strong>Inter</strong> - UI modern, sangat readable</li>
    <li><strong>Poppins</strong> - Ramah dan modern</li>
    <li><strong>Roboto</strong> - Google Material Design</li>
    <li><strong>Plus Jakarta Sans</strong> - Indonesian font, modern</li>
</ul>

<h2>Color Theory untuk UI</h2>
<h3>Skema Warna</h3>
<ul>
    <li><strong>Primary Color</strong> - Warna utama brand (60%)</li>
    <li><strong>Secondary Color</strong> - Pelengkap (30%)</li>
    <li><strong>Accent Color</strong> - Highlight/CTA (10%)</li>
</ul>

<h3>Tips Memilih Warna</h3>
<ol>
    <li>Gunakan maksimal 3 warna utama</li>
    <li>Pastikan kontras cukup (WCAG AA: rasio minimal 4.5:1)</li>
    <li>Jangan gunakan warna merah/hijau bersamaan (color blind)</li>
    <li>Test di grayscale dulu sebelum menambah warna</li>
</ol>"
            },
            new() {
                CourseId = course2.Id, Order = 3, IsPublished = true,
                Title = "Wireframing: Membuat Kerangka Aplikasi",
                Content = @"<h2>Apa itu Wireframe?</h2>
<p>Wireframe adalah blueprint/kerangka kasar dari sebuah halaman atau aplikasi. Dibuat tanpa warna, gambar, atau detail visual — fokus pada struktur dan alur konten.</p>

<h3>Tingkatan Wireframe</h3>
<ol>
    <li><strong>Low-fidelity (Lo-fi)</strong> - Sketsa kasar di kertas atau digital, sangat cepat dibuat</li>
    <li><strong>Mid-fidelity</strong> - Digital, grayscale, sudah ada ukuran dan spacing</li>
    <li><strong>High-fidelity (Hi-fi)</strong> - Sudah mendekati desain final dengan warna dan aset</li>
</ol>

<h3>Komponen Wireframe yang Umum</h3>
<ul>
    <li>Navigation bar (navbar/sidebar)</li>
    <li>Cards dan list items</li>
    <li>Buttons dan form elements</li>
    <li>Modal dan overlay</li>
    <li>Hero section</li>
</ul>

<h3>Tips Membuat Wireframe yang Baik</h3>
<ol>
    <li>Mulai dari mobile terlebih dahulu (mobile-first)</li>
    <li>Gunakan lorem ipsum untuk placeholder teks</li>
    <li>Fokus pada alur pengguna, bukan estetika</li>
    <li>Buat beberapa variasi untuk dipilih</li>
    <li>Validasi dengan stakeholder sebelum lanjut ke mockup</li>
</ol>"
            },
            new() {
                CourseId = course2.Id, Order = 4, IsPublished = true,
                Title = "Prototype Interaktif dan User Testing",
                Content = @"<h2>Membuat Prototype di Figma</h2>
<p>Prototype adalah simulasi interaktif dari desain yang memungkinkan pengguna 'mengklik' dan merasakan alur aplikasi sebelum benar-benar di-develop.</p>

<h3>Cara Membuat Prototype di Figma</h3>
<ol>
    <li>Pilih tab <strong>Prototype</strong> di panel kanan</li>
    <li>Klik elemen yang ingin dibuat interaktif (tombol, link)</li>
    <li>Drag connection ke frame tujuan</li>
    <li>Atur trigger (On Click, On Hover) dan animasi</li>
    <li>Klik tombol ▶ Present untuk preview</li>
</ol>

<h2>User Testing</h2>
<p>User testing adalah proses mengamati pengguna nyata saat menggunakan prototype untuk menemukan masalah usability.</p>

<h3>Metode User Testing</h3>
<ul>
    <li><strong>Moderated Testing</strong> - Peneliti hadir langsung, bisa bertanya</li>
    <li><strong>Unmoderated Testing</strong> - Pengguna tes sendiri, direkam</li>
    <li><strong>A/B Testing</strong> - Bandingkan 2 versi desain</li>
    <li><strong>5-Second Test</strong> - Kesan pertama dalam 5 detik</li>
</ul>

<h3>Task yang Bisa Diuji</h3>
<ol>
    <li>Temukan tombol untuk membuat akun baru</li>
    <li>Tambahkan produk ke keranjang belanja</li>
    <li>Ubah foto profil</li>
    <li>Cari kursus tentang Python</li>
</ol>"
            },
        };
        db.CourseModules.AddRange(modules2);
        await db.SaveChangesAsync();

        db.Assignments.AddRange(
            new Assignment {
                CourseId = course2.Id,
                Title    = "Wireframe Aplikasi Mobile (Low-fidelity)",
                Description = @"Buatlah wireframe low-fidelity untuk aplikasi mobile pilihan kamu di Figma.

**Pilihan aplikasi:**
- Aplikasi belanja online
- Aplikasi belajar bahasa
- Aplikasi manajemen tugas

**Requirement:**
1. Minimal 5 halaman/screen berbeda
2. Terdapat navigasi antar halaman
3. Gunakan komponen standar (navbar, button, input, card)
4. Export sebagai PDF atau share Figma link

**Halaman yang harus ada:**
- Onboarding/Splash screen
- Login/Register
- Home/Dashboard
- Detail halaman
- Profile pengguna",
                DueDate  = DateTime.UtcNow.AddDays(10),
                MaxScore = 100,
            },
            new Assignment {
                CourseId = course2.Id,
                Title    = "High-fidelity UI Design & Prototype",
                Description = @"Kembangkan wireframe dari assignment sebelumnya menjadi desain high-fidelity yang lengkap.

**Requirement:**
1. Gunakan color palette yang konsisten
2. Typography yang proper (hierarki jelas)
3. Komponen yang reusable (gunakan Figma Components)
4. Prototype yang bisa diklik minimal 3 alur interaksi
5. Share Figma link dengan mode Prototype

**Bonus:**
- Dark mode version (+15 poin)
- Design system sederhana (+10 poin)
- Animasi micro-interaction (+5 poin)",
                DueDate  = DateTime.UtcNow.AddDays(21),
                MaxScore = 100,
            }
        );

        var quiz2 = new Quiz {
            CourseId = course2.Id, Title = "Quiz: Prinsip UI/UX Design",
            Description = "Test pengetahuan dasar UI/UX Design.",
            TimeLimitMinutes = 15, MaxAttempts = 2, PassScore = 60, IsPublished = true,
        };
        db.Quizzes.Add(quiz2);
        await db.SaveChangesAsync();

        db.Questions.AddRange(
            new Question {
                QuizId = quiz2.Id, Order = 0, Points = 25,
                Text = "UX Design berfokus pada...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "Tampilan visual yang indah", IsCorrect = false },
                    new() { Text = "Pengalaman dan kemudahan pengguna saat menggunakan produk", IsCorrect = true },
                    new() { Text = "Pemilihan warna yang tepat", IsCorrect = false },
                    new() { Text = "Animasi dan transisi halaman", IsCorrect = false },
                ]
            },
            new Question {
                QuizId = quiz2.Id, Order = 1, Points = 25,
                Text = "Wireframe adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "Desain final dengan warna dan gambar lengkap", IsCorrect = false },
                    new() { Text = "Kode program untuk membuat aplikasi", IsCorrect = false },
                    new() { Text = "Kerangka/blueprint kasar dari tampilan aplikasi", IsCorrect = true },
                    new() { Text = "Dokumen spesifikasi teknis", IsCorrect = false },
                ]
            },
            new Question {
                QuizId = quiz2.Id, Order = 2, Points = 25,
                Text = "Dalam design thinking, tahap 'Empathize' bertujuan untuk...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "Membuat prototype secepat mungkin", IsCorrect = false },
                    new() { Text = "Memahami kebutuhan dan masalah pengguna", IsCorrect = true },
                    new() { Text = "Mendefinisikan fitur aplikasi", IsCorrect = false },
                    new() { Text = "Testing dengan pengguna", IsCorrect = false },
                ]
            },
            new Question {
                QuizId = quiz2.Id, Order = 3, Points = 25,
                Text = "Rasio kontras warna minimum untuk memenuhi standar aksesibilitas WCAG AA adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "2:1", IsCorrect = false },
                    new() { Text = "3:1", IsCorrect = false },
                    new() { Text = "4.5:1", IsCorrect = true },
                    new() { Text = "7:1", IsCorrect = false },
                ]
            }
        );

        db.Announcements.Add(new Announcement {
            CourseId = course2.Id, AuthorId = teacher2.UserId, AuthorName = teacher2.Name,
            Title   = "Selamat Datang di Kursus UI/UX Design!",
            Content = "Halo desainer masa depan! Selamat datang di kursus UI/UX Design dengan Figma. Pastikan kalian sudah membuat akun Figma (gratis) sebelum memulai modul pertama. Link invite ke Figma team akan dikirim via email.",
            IsPinned = true,
        });

        db.Enrollments.AddRange(
            new Enrollment { CourseId = course2.Id, UserId = student1.UserId, UserName = student1.Name, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = course2.Id, UserId = student3.UserId, UserName = student3.Name, Status = EnrollmentStatus.Active }
        );

        await db.SaveChangesAsync();

        // ── Course 3: Digital Marketing ───────────────────────────────────────
        var course3 = new Course
        {
            Title          = "Digital Marketing: SEO, Social Media & Email Marketing",
            Description    = "Kuasai strategi digital marketing modern untuk mengembangkan bisnis atau karir kamu. Dari SEO, social media marketing, email marketing, hingga analisis data. Cocok untuk pemilik bisnis, marketer, dan entrepreneur.",
            Category       = "Marketing",
            Level          = "Semua Level",
            InstructorId   = teacher.UserId,
            InstructorName = teacher.Name,
            IsPublished    = true,
        };
        db.Courses.Add(course3);
        await db.SaveChangesAsync();

        db.CourseModules.AddRange(
            new CourseModule {
                CourseId = course3.Id, Order = 0, IsPublished = true,
                Content = @"<h2>Apa itu Digital Marketing?</h2>
<p>Digital marketing adalah semua upaya pemasaran yang menggunakan internet dan perangkat digital. Ini mencakup berbagai channel seperti mesin pencari, media sosial, email, website, dan iklan online.</p>

<h3>Mengapa Digital Marketing Penting?</h3>
<ul>
    <li>📊 <strong>Terukur</strong> - Setiap klik, view, dan konversi bisa dilacak</li>
    <li>🎯 <strong>Tepat Sasaran</strong> - Target audiens yang sangat spesifik</li>
    <li>💰 <strong>Hemat Biaya</strong> - ROI lebih baik dibanding marketing tradisional</li>
    <li>⚡ <strong>Real-time</strong> - Hasil terlihat cepat, bisa dioptimasi segera</li>
    <li>🌍 <strong>Jangkauan Luas</strong> - Bisa menjangkau pasar global</li>
</ul>

<h3>Channel Digital Marketing Utama</h3>
<ol>
    <li>SEO (Search Engine Optimization)</li>
    <li>SEM (Search Engine Marketing / Google Ads)</li>
    <li>Social Media Marketing</li>
    <li>Email Marketing</li>
    <li>Content Marketing</li>
    <li>Influencer Marketing</li>
    <li>Affiliate Marketing</li>
</ol>"
            },
            new CourseModule {
                CourseId = course3.Id, Order = 1, IsPublished = true,
                Content = @"<h2>Search Engine Optimization (SEO)</h2>
<p>SEO adalah proses mengoptimasi website agar muncul di posisi teratas hasil pencarian Google (organik/gratis).</p>

<h3>Dua Jenis SEO</h3>
<h4>1. On-Page SEO</h4>
<ul>
    <li><strong>Keyword Research</strong> - Temukan kata kunci yang dicari target audiens</li>
    <li><strong>Title Tag</strong> - Judul halaman (maks 60 karakter)</li>
    <li><strong>Meta Description</strong> - Deskripsi singkat (maks 160 karakter)</li>
    <li><strong>Heading Structure</strong> - H1, H2, H3 yang terstruktur</li>
    <li><strong>URL Struktur</strong> - URL yang bersih dan deskriptif</li>
    <li><strong>Image Alt Text</strong> - Deskripsi gambar untuk Google</li>
    <li><strong>Internal Linking</strong> - Hubungkan halaman terkait</li>
</ul>

<h4>2. Off-Page SEO</h4>
<ul>
    <li><strong>Backlinks</strong> - Link dari website lain yang terpercaya</li>
    <li><strong>Social Signals</strong> - Engagement di media sosial</li>
    <li><strong>Brand Mentions</strong> - Sebutan merek di internet</li>
</ul>

<h3>Tools SEO yang Berguna</h3>
<ul>
    <li>Google Search Console (gratis)</li>
    <li>Google Analytics (gratis)</li>
    <li>Ubersuggest (freemium)</li>
    <li>Ahrefs / SEMrush (berbayar)</li>
</ul>"
            },
            new CourseModule {
                CourseId = course3.Id, Order = 2, IsPublished = true,
                Content = @"<h2>Social Media Marketing</h2>
<p>Social media marketing adalah penggunaan platform media sosial untuk membangun brand, meningkatkan traffic, dan mengkonversi leads menjadi pelanggan.</p>

<h3>Platform Utama dan Karakteristiknya</h3>
<table>
    <tr>
        <th>Platform</th><th>Audiens Utama</th><th>Konten Terbaik</th>
    </tr>
    <tr><td>Instagram</td><td>18-34 tahun</td><td>Foto, Reels, Stories</td></tr>
    <tr><td>TikTok</td><td>13-30 tahun</td><td>Video pendek, trending</td></tr>
    <tr><td>LinkedIn</td><td>25-45 tahun (profesional)</td><td>Artikel, carousel, insight</td></tr>
    <tr><td>YouTube</td><td>Semua umur</td><td>Video tutorial, vlog</td></tr>
    <tr><td>Twitter/X</td><td>25-45 tahun</td><td>Opini, news, thread</td></tr>
</table>

<h3>Strategi Konten: Pilar Konten</h3>
<ol>
    <li><strong>Educational (40%)</strong> - Tips, tutorial, how-to</li>
    <li><strong>Entertaining (30%)</strong> - Meme, behind-the-scenes, cerita</li>
    <li><strong>Promotional (20%)</strong> - Promo, produk, testimoni</li>
    <li><strong>Engaging (10%)</strong> - Polls, Q&A, challenge</li>
</ol>

<h3>Content Calendar</h3>
<p>Buat jadwal posting yang konsisten:</p>
<ul>
    <li>Instagram: 1x sehari (minimal 3x seminggu)</li>
    <li>Stories: 3-7x sehari</li>
    <li>TikTok: 1-3x sehari untuk growth</li>
    <li>LinkedIn: 3-5x seminggu</li>
</ul>"
            },
            new CourseModule {
                CourseId = course3.Id, Order = 3, IsPublished = true,
                Content = @"<h2>Email Marketing</h2>
<p>Email marketing memiliki ROI tertinggi di antara semua channel digital marketing: rata-rata $42 untuk setiap $1 yang diinvestasikan.</p>

<h3>Jenis Email Marketing</h3>
<ul>
    <li><strong>Newsletter</strong> - Update reguler tentang konten/produk</li>
    <li><strong>Welcome Series</strong> - Email otomatis saat subscriber baru bergabung</li>
    <li><strong>Promotional Email</strong> - Penawaran diskon, launching produk</li>
    <li><strong>Transactional Email</strong> - Konfirmasi pesanan, reset password</li>
    <li><strong>Re-engagement</strong> - Aktifkan kembali subscriber yang pasif</li>
</ul>

<h3>Anatomi Email yang Efektif</h3>
<ol>
    <li><strong>Subject Line</strong> - Penentu apakah email dibuka (maks 50 karakter)</li>
    <li><strong>Preview Text</strong> - Teaser setelah subject (40-100 karakter)</li>
    <li><strong>Header</strong> - Logo dan visual menarik</li>
    <li><strong>Body</strong> - Konten utama yang jelas dan singkat</li>
    <li><strong>CTA Button</strong> - Satu call-to-action yang jelas</li>
    <li><strong>Footer</strong> - Unsubscribe link, alamat, social media</li>
</ol>

<h3>Tips Subject Line yang Tinggi Open Rate</h3>
<ul>
    <li>Gunakan angka: '5 Tips SEO yang Terbukti'</li>
    <li>Pertanyaan: 'Sudahkah kamu melakukan ini?'</li>
    <li>Personalisasi: 'Budi, ada penawaran spesial untukmu'</li>
    <li>Urgency: 'Promo berakhir malam ini!'</li>
    <li>Curiosity gap: 'Satu kesalahan yang membuat iklanmu gagal'</li>
</ul>"
            },
            new CourseModule {
                CourseId = course3.Id, Order = 4, IsPublished = true,
                Title = "Analytics: Mengukur dan Mengoptimasi Kampanye",
                Content = @"<h2>Digital Marketing Analytics</h2>
<p>Data adalah fondasi dari digital marketing yang sukses. Tanpa mengukur, kamu tidak bisa mengoptimasi.</p>

<h3>Metrik Penting per Channel</h3>
<h4>Website (Google Analytics)</h4>
<ul>
    <li><strong>Sessions & Users</strong> - Jumlah kunjungan</li>
    <li><strong>Bounce Rate</strong> - % pengunjung yang langsung pergi (idealnya &lt; 50%)</li>
    <li><strong>Average Session Duration</strong> - Lama kunjungan rata-rata</li>
    <li><strong>Conversion Rate</strong> - % pengunjung yang melakukan aksi</li>
</ul>

<h4>Social Media</h4>
<ul>
    <li><strong>Reach & Impressions</strong> - Berapa orang yang melihat konten</li>
    <li><strong>Engagement Rate</strong> - (Like+Comment+Share)/Reach × 100</li>
    <li><strong>Follower Growth Rate</strong> - Pertumbuhan follower bulanan</li>
    <li><strong>Click-Through Rate (CTR)</strong> - % yang klik link</li>
</ul>

<h4>Email Marketing</h4>
<ul>
    <li><strong>Open Rate</strong> - % email yang dibuka (benchmark: 20-25%)</li>
    <li><strong>Click Rate</strong> - % yang klik CTA (benchmark: 2-5%)</li>
    <li><strong>Unsubscribe Rate</strong> - Harus &lt; 0.5%</li>
    <li><strong>Conversion Rate</strong> - Yang melakukan pembelian/aksi</li>
</ul>

<h3>Framework AARRR (Pirate Metrics)</h3>
<ol>
    <li><strong>Acquisition</strong> - Bagaimana pengguna menemukan kamu?</li>
    <li><strong>Activation</strong> - Apakah mereka memiliki pengalaman pertama yang baik?</li>
    <li><strong>Retention</strong> - Apakah mereka kembali lagi?</li>
    <li><strong>Revenue</strong> - Apakah mereka membayar?</li>
    <li><strong>Referral</strong> - Apakah mereka merekomendasikan ke orang lain?</li>
</ol>"
            }
        );

        var quiz3 = new Quiz {
            CourseId = course3.Id, Title = "Quiz: Dasar-dasar Digital Marketing",
            Description = "Uji pemahaman kamu tentang konsep dasar digital marketing.",
            TimeLimitMinutes = 15, MaxAttempts = 2, PassScore = 60, IsPublished = true,
        };
        db.Quizzes.Add(quiz3);
        await db.SaveChangesAsync();

        db.Questions.AddRange(
            new Question {
                QuizId = quiz3.Id, Order = 0, Points = 25,
                Text = "Kepanjangan dari SEO adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "Social Engagement Optimization", IsCorrect = false },
                    new() { Text = "Search Engine Optimization", IsCorrect = true },
                    new() { Text = "Search Engine Operation", IsCorrect = false },
                    new() { Text = "Site Engine Optimization", IsCorrect = false },
                ]
            },
            new Question {
                QuizId = quiz3.Id, Order = 1, Points = 25,
                Text = "ROI rata-rata email marketing untuk setiap $1 yang diinvestasikan adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "$5", IsCorrect = false },
                    new() { Text = "$15", IsCorrect = false },
                    new() { Text = "$42", IsCorrect = true },
                    new() { Text = "$100", IsCorrect = false },
                ]
            },
            new Question {
                QuizId = quiz3.Id, Order = 2, Points = 25,
                Text = "Bounce rate yang ideal untuk sebuah website adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "Di atas 80%", IsCorrect = false },
                    new() { Text = "Di bawah 50%", IsCorrect = true },
                    new() { Text = "Tepat 100%", IsCorrect = false },
                    new() { Text = "Di bawah 5%", IsCorrect = false },
                ]
            },
            new Question {
                QuizId = quiz3.Id, Order = 3, Points = 25,
                Text = "Pilar konten yang paling banyak direkomendasikan untuk social media adalah...",
                Type = QuestionType.MultipleChoice,
                Options = [
                    new() { Text = "100% konten promosi produk", IsCorrect = false },
                    new() { Text = "Educational, entertaining, promotional, dan engaging", IsCorrect = true },
                    new() { Text = "Hanya konten viral dan trending", IsCorrect = false },
                    new() { Text = "50% promosi, 50% hiburan", IsCorrect = false },
                ]
            }
        );

        db.Assignments.AddRange(
            new Assignment {
                CourseId = course3.Id,
                Title    = "Riset Keyword dan Analisis Kompetitor SEO",
                Description = @"Pilih satu bisnis/brand (boleh bisnis sendiri atau brand terkenal) dan lakukan:

1. **Keyword Research:**
   - Temukan 10 keyword utama yang relevan
   - Identifikasi search volume dan tingkat kesulitan (gunakan Ubersuggest gratis)
   - Kelompokkan: informational, navigational, transactional

2. **Analisis Kompetitor:**
   - Identifikasi 3 kompetitor utama di Google
   - Analisis title tag dan meta description mereka
   - Apa yang bisa diperbaiki?

3. **Rekomendasi:**
   - 5 rekomendasi konkret untuk meningkatkan SEO

**Format:** Laporan dalam format PDF atau Google Docs (share link)",
                DueDate = DateTime.UtcNow.AddDays(7), MaxScore = 100,
            },
            new Assignment {
                CourseId = course3.Id,
                Title    = "Buat Konten Plan Social Media 1 Bulan",
                Description = @"Buat content plan social media selama 1 bulan untuk brand/bisnis pilihanmu.

**Yang harus dibuat:**
1. Tentukan platform (pilih 1: Instagram atau LinkedIn)
2. Tentukan target audiens dan buyer persona
3. Buat content calendar 4 minggu (minimal 3 post/minggu)
4. Untuk setiap post: tema, caption draft, hashtag, dan waktu posting
5. Buat 2 contoh konten lengkap (caption + brief desain)

**Bonus:** Buat 1 konten actual dan posting, share link ke pengumpulan (+20 poin)",
                DueDate = DateTime.UtcNow.AddDays(14), MaxScore = 100,
            }
        );

        db.Announcements.Add(new Announcement {
            CourseId = course3.Id, AuthorId = teacher.UserId, AuthorName = teacher.Name,
            Title   = "Welcome & Persiapan Kursus Digital Marketing",
            Content = @"Selamat datang di kursus Digital Marketing!

Sebelum memulai, pastikan kamu sudah:
✅ Membuat akun Google Analytics (gratis)
✅ Install Google Search Console untuk website kamu (jika punya)
✅ Membuat akun Ubersuggest untuk riset keyword
✅ Bergabung ke grup WhatsApp kursus (link di sidebar)

Jadwal live session: Setiap Sabtu pukul 19.00 WIB via Zoom. Link akan dikirim H-1.",
            IsPinned = true,
        });

        db.ForumPosts.AddRange(
            new ForumPost {
                CourseId = course3.Id, UserId = student2.UserId, UserName = student2.Name,
                Title = "Tools SEO gratis yang paling recommended?",
                Body  = "Kak, saya masih pemula dan belum bisa berlangganan tools berbayar seperti Ahrefs. Tools SEO gratis apa yang paling recommended untuk belajar? Terima kasih!",
            },
            new ForumPost {
                CourseId = course3.Id, UserId = student4.UserId, UserName = student4.Name,
                Title = "Berapa frekuensi posting Instagram yang ideal?",
                Body  = "Saya sering dengar berbeda-beda pendapat soal frekuensi posting. Ada yang bilang harus setiap hari, ada yang bilang kualitas lebih penting dari kuantitas. Pendapat kakak bagaimana?",
            }
        );

        db.Enrollments.AddRange(
            new Enrollment { CourseId = course3.Id, UserId = student2.UserId, UserName = student2.Name, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = course3.Id, UserId = student4.UserId, UserName = student4.Name, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = course3.Id, UserId = student1.UserId, UserName = student1.Name, Status = EnrollmentStatus.Active }
        );

        db.CourseReviews.Add(new CourseReview {
            CourseId = course3.Id, UserId = student2.UserId, UserName = student2.Name,
            Rating = 5, Comment = "Materi sangat praktis dan langsung bisa diterapkan. Penjelasan tentang SEO sangat membantu bisnis saya. Terima kasih!"
        });

        await db.SaveChangesAsync();

        Console.WriteLine("[Seeder] ✅ Data contoh berhasil dibuat: 3 kursus, modul, quiz, assignment, enrollment.");

        // ── Seed Question Bank ─────────────────────────────────────────────────
        var bankQuestions = new List<QuestionBank>
        {
            new() {
                OwnerId = teacher.UserId, OwnerName = teacher.Name, Category = "Pemrograman Web",
                Text = "Apa kepanjangan dari HTML?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "HyperText Markup Language", IsCorrect = true },
                    new() { Text = "HighText Machine Language", IsCorrect = false },
                    new() { Text = "HyperText and links Markup Language", IsCorrect = false },
                    new() { Text = "HyperText Makeup Language", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher.UserId, OwnerName = teacher.Name, Category = "Pemrograman Web",
                Text = "CSS singkatan dari apa?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "Cascading Style Sheets", IsCorrect = true },
                    new() { Text = "Creative Style System", IsCorrect = false },
                    new() { Text = "Colorful Style Sheets", IsCorrect = false },
                    new() { Text = "Computer Style Sheets", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher.UserId, OwnerName = teacher.Name, Category = "Pemrograman Web",
                Text = "JavaScript adalah bahasa pemrograman yang berjalan di sisi client (browser).", Type = QuestionType.TrueFalse, Points = 5,
                Options = [
                    new() { Text = "Benar", IsCorrect = true },
                    new() { Text = "Salah", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher.UserId, OwnerName = teacher.Name, Category = "Pemrograman Web",
                Text = "Tag HTML mana yang digunakan untuk membuat hyperlink?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "<a>", IsCorrect = true },
                    new() { Text = "<link>", IsCorrect = false },
                    new() { Text = "<href>", IsCorrect = false },
                    new() { Text = "<url>", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher.UserId, OwnerName = teacher.Name, Category = "Pemrograman Web",
                Text = "Property CSS apa yang digunakan untuk mengubah warna latar belakang?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "background-color", IsCorrect = true },
                    new() { Text = "bg-color", IsCorrect = false },
                    new() { Text = "color-background", IsCorrect = false },
                    new() { Text = "bgcolor", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher2.UserId, OwnerName = teacher2.Name, Category = "Basis Data",
                Text = "Perintah SQL yang digunakan untuk mengambil data dari tabel adalah?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "SELECT", IsCorrect = true },
                    new() { Text = "GET", IsCorrect = false },
                    new() { Text = "FETCH", IsCorrect = false },
                    new() { Text = "RETRIEVE", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher2.UserId, OwnerName = teacher2.Name, Category = "Basis Data",
                Text = "SQL adalah singkatan dari Structured Query Language.", Type = QuestionType.TrueFalse, Points = 5,
                Options = [
                    new() { Text = "Benar", IsCorrect = true },
                    new() { Text = "Salah", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher2.UserId, OwnerName = teacher2.Name, Category = "Basis Data",
                Text = "Klausa SQL mana yang digunakan untuk memfilter hasil query?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "WHERE", IsCorrect = true },
                    new() { Text = "FILTER", IsCorrect = false },
                    new() { Text = "HAVING", IsCorrect = false },
                    new() { Text = "LIMIT", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher.UserId, OwnerName = teacher.Name, Category = "Algoritma",
                Text = "Struktur data LIFO (Last In First Out) dikenal sebagai?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "Stack", IsCorrect = true },
                    new() { Text = "Queue", IsCorrect = false },
                    new() { Text = "Array", IsCorrect = false },
                    new() { Text = "Linked List", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher.UserId, OwnerName = teacher.Name, Category = "Algoritma",
                Text = "Array memiliki ukuran yang bisa berubah-ubah secara dinamis.", Type = QuestionType.TrueFalse, Points = 5,
                Options = [
                    new() { Text = "Benar", IsCorrect = false },
                    new() { Text = "Salah", IsCorrect = true }
                ]
            },
            new() {
                OwnerId = teacher2.UserId, OwnerName = teacher2.Name, Category = "Jaringan",
                Text = "Protokol yang digunakan untuk mengakses website adalah?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "HTTP/HTTPS", IsCorrect = true },
                    new() { Text = "FTP", IsCorrect = false },
                    new() { Text = "SMTP", IsCorrect = false },
                    new() { Text = "SSH", IsCorrect = false }
                ]
            },
            new() {
                OwnerId = teacher2.UserId, OwnerName = teacher2.Name, Category = "Jaringan",
                Text = "IP Address versi 4 (IPv4) terdiri dari berapa bit?", Type = QuestionType.MultipleChoice, Points = 10,
                Options = [
                    new() { Text = "32 bit", IsCorrect = true },
                    new() { Text = "64 bit", IsCorrect = false },
                    new() { Text = "16 bit", IsCorrect = false },
                    new() { Text = "128 bit", IsCorrect = false }
                ]
            },
        };
        db.QuestionBank.AddRange(bankQuestions);
        await db.SaveChangesAsync();

        // ── Seed Practice Quiz ─────────────────────────────────────────────────
        db.PracticeQuizzes.Add(new PracticeQuiz
        {
            Title           = "Latihan Umum Teknologi Informasi",
            Description     = "10 soal acak dari berbagai kategori: Web, Database, Algoritma, dan Jaringan.",
            QuestionCount   = 10,
            ShuffleQuestions = true,
            ShuffleOptions  = true,
            TimeLimitMinutes = 15,
            CreatedBy       = teacher.UserId,
            CreatedByName   = teacher.Name
        });
        db.PracticeQuizzes.Add(new PracticeQuiz
        {
            Title           = "Kuis Cepat Pemrograman Web",
            Description     = "5 soal fokus pada HTML, CSS, dan JavaScript.",
            QuestionCount   = 5,
            ShuffleQuestions = true,
            ShuffleOptions  = true,
            TimeLimitMinutes = 10,
            CreatedBy       = teacher.UserId,
            CreatedByName   = teacher.Name
        });
        await db.SaveChangesAsync();

        Console.WriteLine("[Seeder] ✅ Bank soal (12 soal) + 2 practice quiz berhasil dibuat.");
    }

    private static async Task<AppUser> CreateUser(
        LmsDbContext db, string userId, string name, string email, string role)
    {
        var existing = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        if (existing != null) return existing;

        // Cek apakah email sudah ada (dari Keycloak sync)
        var byEmail = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (byEmail != null) return byEmail;

        var user = new AppUser { UserId = userId, Name = name, Email = email, Role = role };
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
