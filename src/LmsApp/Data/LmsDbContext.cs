using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Data;

public class LmsDbContext(DbContextOptions<LmsDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Course
        modelBuilder.Entity<Course>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Title).HasMaxLength(200).IsRequired();
            e.Property(c => c.InstructorId).HasMaxLength(100).IsRequired();
            e.HasMany(c => c.Enrollments).WithOne(en => en.Course).HasForeignKey(en => en.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Assignments).WithOne(a => a.Course).HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Modules).WithOne(m => m.Course).HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        // Enrollment - unique per user per course
        modelBuilder.Entity<Enrollment>(e =>
        {
            e.HasKey(en => en.Id);
            e.HasIndex(en => new { en.CourseId, en.UserId }).IsUnique();
            e.Property(en => en.UserId).HasMaxLength(100).IsRequired();
        });

        // Assignment
        modelBuilder.Entity<Assignment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Title).HasMaxLength(200).IsRequired();
            e.HasMany(a => a.Submissions).WithOne(s => s.Assignment).HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        });

        // CourseModule
        modelBuilder.Entity<CourseModule>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Title).HasMaxLength(200).IsRequired();
        });

        // Submission
        modelBuilder.Entity<Submission>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.UserId).HasMaxLength(100).IsRequired();
        });
    }
}
