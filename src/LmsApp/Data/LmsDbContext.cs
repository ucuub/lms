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

    // Forum
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<CourseReview> CourseReviews => Set<CourseReview>();

    // Engagement
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<CourseProgress> CourseProgresses => Set<CourseProgress>();
    public DbSet<ModuleProgress> ModuleProgresses => Set<ModuleProgress>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();

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
            e.HasIndex(f => new { f.CourseId, f.ParentId, f.CreatedAt });
        });

        // Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.Property(n => n.UserId).HasMaxLength(100).IsRequired();
        });
    }
}
