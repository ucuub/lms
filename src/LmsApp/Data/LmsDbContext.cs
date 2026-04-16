using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Data;

public class LmsDbContext(DbContextOptions<LmsDbContext> options) : DbContext(options)
{
    // Users
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    // Core
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSection> CourseSections => Set<CourseSection>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<CourseCompletionRule> CourseCompletionRules => Set<CourseCompletionRule>();
    public DbSet<ModuleAttachment> ModuleAttachments => Set<ModuleAttachment>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    // Quiz
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();

    // Question Bank
    public DbSet<QuestionBank> QuestionBank => Set<QuestionBank>();
    public DbSet<QuestionBankOption> QuestionBankOptions => Set<QuestionBankOption>();

    // Prerequisites
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();

    // Attendance
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords   => Set<AttendanceRecord>();

    // Resources
    public DbSet<CourseResource> CourseResources => Set<CourseResource>();

    // Forum
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<ForumLike> ForumLikes => Set<ForumLike>();
    public DbSet<CourseReview> CourseReviews => Set<CourseReview>();

    // Gradebook
    public DbSet<CourseGradeItem> CourseGradeItems => Set<CourseGradeItem>();
    public DbSet<CourseGradeEntry> CourseGradeEntries => Set<CourseGradeEntry>();

    // Engagement
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<CourseProgress> CourseProgresses => Set<CourseProgress>();
    public DbSet<ModuleProgress> ModuleProgresses => Set<ModuleProgress>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Messaging
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    // Activity Log
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    // Practice Quiz (standalone — terpisah dari Course Quiz)
    public DbSet<PracticeQuiz> PracticeQuizzes => Set<PracticeQuiz>();
    public DbSet<PracticeAttempt> PracticeAttempts => Set<PracticeAttempt>();
    public DbSet<PracticeAttemptAnswer> PracticeAttemptAnswers => Set<PracticeAttemptAnswer>();

    // Standalone Exam (admin buat, semua user kerjakan)
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamQuestionOption> ExamQuestionOptions => Set<ExamQuestionOption>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<ExamAnswer> ExamAnswers => Set<ExamAnswer>();

    // Question Set (teacher/admin buat, bisa import dari bank soal)
    public DbSet<QuestionSet> QuestionSets => Set<QuestionSet>();
    public DbSet<QuestionSetQuestion> QuestionSetQuestions => Set<QuestionSetQuestion>();
    public DbSet<QuestionSetOption> QuestionSetOptions => Set<QuestionSetOption>();
    public DbSet<QuestionSetAttempt> QuestionSetAttempts => Set<QuestionSetAttempt>();
    public DbSet<QuestionSetAnswer> QuestionSetAnswers => Set<QuestionSetAnswer>();

    // Mandatory Exam (deep-link, per-user assignment)
    public DbSet<MandatoryExam> MandatoryExams => Set<MandatoryExam>();
    public DbSet<MandatoryExamQuestion> MandatoryExamQuestions => Set<MandatoryExamQuestion>();
    public DbSet<MandatoryExamOption> MandatoryExamOptions => Set<MandatoryExamOption>();
    public DbSet<MandatoryExamAssignment> MandatoryExamAssignments => Set<MandatoryExamAssignment>();
    public DbSet<MandatoryExamAttempt> MandatoryExamAttempts => Set<MandatoryExamAttempt>();
    public DbSet<MandatoryExamAnswer> MandatoryExamAnswers => Set<MandatoryExamAnswer>();
    public DbSet<MandatoryExamSession> MandatoryExamSessions => Set<MandatoryExamSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.UserId).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.UserId).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.Role).HasMaxLength(50).HasDefaultValue("student");
        });

        // Course
        modelBuilder.Entity<Course>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Title).HasMaxLength(200).IsRequired();
            e.HasMany(c => c.Enrollments).WithOne(en => en.Course).HasForeignKey(en => en.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Assignments).WithOne(a => a.Course).HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Modules).WithOne(m => m.Course).HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Quizzes).WithOne(q => q.Course).HasForeignKey(q => q.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Announcements).WithOne(a => a.Course).HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.ForumPosts).WithOne(f => f.Course).HasForeignKey(f => f.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Reviews).WithOne(r => r.Course).HasForeignKey(r => r.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        // Enrollment
        modelBuilder.Entity<Enrollment>(e =>
        {
            e.HasKey(en => en.Id);
            e.HasIndex(en => new { en.CourseId, en.UserId }).IsUnique();
        });

        // CourseSection
        modelBuilder.Entity<CourseSection>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).HasMaxLength(200).IsRequired();

            // Section → Course (cascade: hapus course → hapus semua section-nya)
            e.HasOne(s => s.Course)
                .WithMany(c => c.Sections)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index untuk ordering query yang efisien
            e.HasIndex(s => new { s.CourseId, s.Order });
        });

        // CourseModule
        modelBuilder.Entity<CourseModule>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Title).HasMaxLength(200).IsRequired();

            // Module → Section (SetNull: hapus section → SectionId modul jadi null)
            // Dengan begini modul tidak ikut terhapus saat section dihapus.
            e.HasOne(m => m.Section)
                .WithMany(s => s.Modules)
                .HasForeignKey(m => m.SectionId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(m => m.Attachments)
                .WithOne(a => a.Module)
                .HasForeignKey(a => a.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModuleAttachment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        });

        // Quiz
        modelBuilder.Entity<Quiz>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasMany(q => q.Questions).WithOne(qu => qu.Quiz).HasForeignKey(qu => qu.QuizId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(q => q.Attempts).WithOne(a => a.Quiz).HasForeignKey(a => a.QuizId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasMany(q => q.Options).WithOne(o => o.Question).HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttempt>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasMany(a => a.Answers).WithOne(ans => ans.Attempt).HasForeignKey(ans => ans.AttemptId).OnDelete(DeleteBehavior.Cascade);
        });

        // Question Bank
        modelBuilder.Entity<QuestionBank>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.OwnerId).HasMaxLength(100).IsRequired();
            e.HasMany(q => q.Options).WithOne(o => o.QuestionBank).HasForeignKey(o => o.QuestionBankId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionBankOption>(e =>
        {
            e.HasKey(o => o.Id);
        });

        // Progress
        modelBuilder.Entity<CourseProgress>(e =>
        {
            e.HasKey(p => p.Id);
            e.Ignore(p => p.Percentage);
            e.HasIndex(p => new { p.CourseId, p.UserId }).IsUnique();
            e.HasMany(p => p.ModuleProgresses).WithOne(mp => mp.CourseProgress).HasForeignKey(mp => mp.CourseProgressId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModuleProgress>(e =>
        {
            e.HasKey(mp => mp.Id);
            e.HasIndex(mp => new { mp.ModuleId, mp.UserId }).IsUnique();
        });

        // Certificate
        modelBuilder.Entity<Certificate>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.CourseId, c.UserId }).IsUnique();
            e.HasIndex(c => c.CertificateNumber).IsUnique();
            e.Property(c => c.CertificateNumber).HasMaxLength(50);
            e.HasOne(c => c.Course)
                .WithMany(co => co.Certificates)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CourseResource
        modelBuilder.Entity<CourseResource>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Title).HasMaxLength(200).IsRequired();
            e.Property(r => r.UploadedBy).HasMaxLength(100);
            e.HasOne(r => r.Course)
                .WithMany()
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.CourseId, r.Order });
        });

        // Attendance
        modelBuilder.Entity<AttendanceSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).HasMaxLength(200).IsRequired();
            e.HasOne(s => s.Course)
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.Records)
                .WithOne(r => r.Session)
                .HasForeignKey(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => new { s.CourseId, s.SessionDate });
        });

        modelBuilder.Entity<AttendanceRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.UserId).HasMaxLength(100).IsRequired();
            // Satu user hanya boleh punya satu record per sesi
            e.HasIndex(r => new { r.SessionId, r.UserId }).IsUnique();
        });

        // CoursePrerequisite
        modelBuilder.Entity<CoursePrerequisite>(e =>
        {
            e.HasKey(p => p.Id);
            // Unique: satu pasangan (CourseId, PrerequisiteCourseId) hanya boleh ada sekali
            e.HasIndex(p => new { p.CourseId, p.PrerequisiteCourseId }).IsUnique();
            e.HasOne(p => p.Course)
                .WithMany(c => c.Prerequisites)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.PrerequisiteCourse)
                .WithMany()
                .HasForeignKey(p => p.PrerequisiteCourseId)
                .OnDelete(DeleteBehavior.Restrict); // jangan hapus course jika masih jadi prerequisite
        });

        // CourseCompletionRule — one-to-one dengan Course
        modelBuilder.Entity<CourseCompletionRule>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.CourseId).IsUnique(); // satu rule per course
            e.HasOne(r => r.Course)
                .WithOne(c => c.CompletionRule)
                .HasForeignKey<CourseCompletionRule>(r => r.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Assignment + Submission
        modelBuilder.Entity<Assignment>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasMany(a => a.Submissions).WithOne(s => s.Assignment).HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Submission>(e =>
        {
            e.HasKey(s => s.Id);
            // Satu student hanya boleh punya satu submission per assignment
            e.HasIndex(s => new { s.AssignmentId, s.UserId }).IsUnique();
            e.Property(s => s.UserId).HasMaxLength(100).IsRequired();
        });

        // CourseGradeItem + CourseGradeEntry
        modelBuilder.Entity<CourseGradeItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).HasMaxLength(200).IsRequired();
            e.HasOne(i => i.Course)
                .WithMany(c => c.GradeItems)
                .HasForeignKey(i => i.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.Entries)
                .WithOne(en => en.GradeItem)
                .HasForeignKey(en => en.GradeItemId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(i => new { i.CourseId, i.Order });
        });

        modelBuilder.Entity<CourseGradeEntry>(e =>
        {
            e.HasKey(en => en.Id);
            e.HasIndex(en => new { en.GradeItemId, en.UserId }).IsUnique();
            e.Property(en => en.UserId).HasMaxLength(100).IsRequired();
        });

        // CalendarEvent
        modelBuilder.Entity<CalendarEvent>(e =>
        {
            e.HasKey(ce => ce.Id);
        });

        // CourseReview
        modelBuilder.Entity<CourseReview>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.CourseId, r.UserId }).IsUnique();
            e.Property(r => r.UserId).HasMaxLength(100).IsRequired();
        });

        // Forum
        modelBuilder.Entity<ForumPost>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Title).HasMaxLength(300);
            e.Property(f => f.UserId).HasMaxLength(100).IsRequired();
            e.HasOne(f => f.Parent)
                .WithMany(f => f.Replies)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            // Index for thread-list queries (pinned + date ordering)
            e.HasIndex(f => new { f.CourseId, f.ParentId, f.CreatedAt });
            // Index for loading all replies in a thread via RootThreadId
            e.HasIndex(f => new { f.RootThreadId, f.CreatedAt });
        });

        modelBuilder.Entity<ForumLike>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.UserId).HasMaxLength(100).IsRequired();
            // One like per user per post
            e.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();
            e.HasOne(l => l.Post)
                .WithMany(f => f.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.Property(n => n.UserId).HasMaxLength(100).IsRequired();
        });

        // Conversation
        modelBuilder.Entity<Conversation>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.User1Id).HasMaxLength(100).IsRequired();
            e.Property(c => c.User2Id).HasMaxLength(100).IsRequired();
            e.HasIndex(c => new { c.User1Id, c.User2Id });
            e.HasMany(c => c.Messages).WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        });

        // Message
        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Content).IsRequired();
            e.Property(m => m.SenderId).HasMaxLength(100).IsRequired();
            e.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        });

        // ActivityLog
        modelBuilder.Entity<ActivityLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).HasMaxLength(100).IsRequired();
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            e.HasIndex(a => new { a.UserId, a.Timestamp });
            e.HasIndex(a => new { a.CourseId, a.Timestamp });
        });

        // PracticeQuiz — standalone, tidak terkait Course
        modelBuilder.Entity<PracticeQuiz>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).HasMaxLength(200).IsRequired();
            e.Property(p => p.CreatedBy).HasMaxLength(100).IsRequired();
            e.HasMany(p => p.Attempts)
                .WithOne(a => a.PracticeQuiz)
                .HasForeignKey(a => a.PracticeQuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PracticeAttempt>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).HasMaxLength(100).IsRequired();
            e.HasIndex(a => new { a.PracticeQuizId, a.UserId });
            e.HasMany(a => a.Answers)
                .WithOne(ans => ans.Attempt)
                .HasForeignKey(ans => ans.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PracticeAttemptAnswer>(e =>
        {
            e.HasKey(ans => ans.Id);
            // FK ke QuestionBank — jangan cascade delete (bank soal tidak terkait quiz ini)
            e.HasOne(ans => ans.BankQuestion)
                .WithMany()
                .HasForeignKey(ans => ans.BankQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            // FK ke QuestionBankOption (nullable)
            e.HasOne(ans => ans.SelectedOption)
                .WithMany()
                .HasForeignKey(ans => ans.SelectedOptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Standalone Exam
        modelBuilder.Entity<Exam>(e =>
        {
            e.HasKey(ex => ex.Id);
            e.Property(ex => ex.Title).HasMaxLength(200).IsRequired();
            e.Property(ex => ex.CreatedBy).HasMaxLength(100).IsRequired();
            e.HasMany(ex => ex.Questions)
                .WithOne(q => q.Exam)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(ex => ex.Attempts)
                .WithOne(a => a.Exam)
                .HasForeignKey(a => a.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamQuestion>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamQuestionOption>(e => e.HasKey(o => o.Id));

        modelBuilder.Entity<ExamAttempt>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).HasMaxLength(100).IsRequired();
            e.HasIndex(a => new { a.ExamId, a.UserId });
            e.HasMany(a => a.Answers)
                .WithOne(ans => ans.Attempt)
                .HasForeignKey(ans => ans.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamAnswer>(e =>
        {
            e.HasKey(ans => ans.Id);
            e.HasOne(ans => ans.Question)
                .WithMany()
                .HasForeignKey(ans => ans.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Question Set — teacher/admin buat, bisa import dari bank soal
        modelBuilder.Entity<QuestionSet>(e =>
        {
            e.HasKey(qs => qs.Id);
            e.Property(qs => qs.Title).HasMaxLength(200).IsRequired();
            e.Property(qs => qs.CreatedBy).HasMaxLength(100).IsRequired();
            e.HasMany(qs => qs.Questions)
                .WithOne(q => q.QuestionSet)
                .HasForeignKey(q => q.QuestionSetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(qs => qs.Attempts)
                .WithOne(a => a.QuestionSet)
                .HasForeignKey(a => a.QuestionSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionSetQuestion>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionSetOption>(e => e.HasKey(o => o.Id));

        modelBuilder.Entity<QuestionSetAttempt>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).HasMaxLength(100).IsRequired();
            e.HasIndex(a => new { a.QuestionSetId, a.UserId });
            e.HasMany(a => a.Answers)
                .WithOne(ans => ans.Attempt)
                .HasForeignKey(ans => ans.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionSetAnswer>(e =>
        {
            e.HasKey(ans => ans.Id);
            e.HasOne(ans => ans.Question)
                .WithMany()
                .HasForeignKey(ans => ans.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Mandatory Exam ────────────────────────────────────────────────────

        modelBuilder.Entity<MandatoryExam>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Questions)
                .WithOne(q => q.Exam)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Assignments)
                .WithOne(a => a.Exam)
                .HasForeignKey(a => a.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MandatoryExamQuestion>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MandatoryExamOption>(e => e.HasKey(o => o.Id));

        modelBuilder.Entity<MandatoryExamAssignment>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.ExamId, a.UserId }).IsUnique();
            e.HasMany(a => a.Attempts)
                .WithOne(t => t.Assignment)
                .HasForeignKey(t => t.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MandatoryExamAttempt>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasMany(t => t.Answers)
                .WithOne(ans => ans.Attempt)
                .HasForeignKey(ans => ans.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MandatoryExamAnswer>(e =>
        {
            e.HasKey(ans => ans.Id);
            e.HasOne(ans => ans.Question)
                .WithMany()
                .HasForeignKey(ans => ans.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MandatoryExamSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.TokenJti).IsUnique();
            e.HasOne(s => s.Exam)
                .WithMany()
                .HasForeignKey(s => s.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
