using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

public class GradebookService(LmsDbContext db) : IGradebookService
{
    // ── Grade Item Management ─────────────────────────────────────────────────

    public async Task<List<GradeItemConfigDto>> GetGradeItemsAsync(int courseId)
    {
        var items = await db.CourseGradeItems
            .Where(i => i.CourseId == courseId)
            .OrderBy(i => i.Order)
            .ThenBy(i => i.Id)
            .ToListAsync();

        return items.Select(ToConfigDto).ToList();
    }

    public async Task<GradeItemConfigDto> CreateGradeItemAsync(int courseId, CreateGradeItemRequest req, string createdByUserId)
    {
        var type = Enum.Parse<GradeItemType>(req.Type, ignoreCase: true);

        var item = new CourseGradeItem
        {
            CourseId = courseId,
            Name = req.Name,
            Type = type,
            MaxScore = req.MaxScore,
            Weight = req.Weight,
            Order = req.Order,
            IsVisible = req.IsVisible,
            AssignmentId = req.AssignmentId,
            QuizId = req.QuizId
        };
        db.CourseGradeItems.Add(item);
        await db.SaveChangesAsync();
        return ToConfigDto(item);
    }

    public async Task<GradeItemConfigDto?> UpdateWeightAsync(int gradeItemId, double weight, string requestUserId)
    {
        var item = await db.CourseGradeItems.FindAsync(gradeItemId);
        if (item == null) return null;

        item.Weight = weight;
        await db.SaveChangesAsync();
        return ToConfigDto(item);
    }

    public async Task<bool> DeleteGradeItemAsync(int gradeItemId, string requestUserId)
    {
        var item = await db.CourseGradeItems.FindAsync(gradeItemId);
        if (item == null) return false;

        db.CourseGradeItems.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    // ── Manual Grading ────────────────────────────────────────────────────────

    public async Task<CourseGradeEntry> SetManualGradeAsync(int gradeItemId, SetManualGradeRequest req, string gradedByUserId)
    {
        var entry = await db.CourseGradeEntries
            .FirstOrDefaultAsync(e => e.GradeItemId == gradeItemId && e.UserId == req.UserId);

        if (entry == null)
        {
            entry = new CourseGradeEntry
            {
                GradeItemId = gradeItemId,
                UserId = req.UserId,
                UserName = req.UserName,
                Score = req.Score,
                Comment = req.Comment,
                GradedByUserId = gradedByUserId
            };
            db.CourseGradeEntries.Add(entry);
        }
        else
        {
            entry.Score = req.Score;
            entry.Comment = req.Comment;
            entry.GradedAt = DateTime.UtcNow;
            entry.GradedByUserId = gradedByUserId;
        }

        await db.SaveChangesAsync();
        return entry;
    }

    // ── Gradebook Views ───────────────────────────────────────────────────────

    public async Task<WeightedGradebookView?> GetStudentViewAsync(int courseId, string userId)
    {
        var course = await db.Courses.FindAsync(courseId);
        if (course == null) return null;

        var items = await db.CourseGradeItems
            .Where(i => i.CourseId == courseId && i.IsVisible)
            .OrderBy(i => i.Order).ThenBy(i => i.Id)
            .ToListAsync();

        var submissionsByAssignment = await db.Submissions
            .Where(s => s.Assignment.CourseId == courseId && s.UserId == userId && s.Score != null)
            .ToListAsync();

        var attemptsByQuiz = await db.QuizAttempts
            .Where(a => a.Quiz.CourseId == courseId && a.UserId == userId && a.SubmittedAt != null)
            .ToListAsync();

        var manualEntries = await db.CourseGradeEntries
            .Where(e => e.GradeItem.CourseId == courseId && e.UserId == userId)
            .ToListAsync();

        var views = items.Select(item => BuildItemView(item, userId,
            submissionsByAssignment, attemptsByQuiz, manualEntries)).ToList();

        var (avg, letter) = ComputeWeightedAverage(views);

        return new WeightedGradebookView(courseId, course.Title, views, avg, letter);
    }

    public async Task<List<WeightedStudentRow>> GetTeacherViewAsync(int courseId)
    {
        var enrollments = await db.Enrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

        var items = await db.CourseGradeItems
            .Where(i => i.CourseId == courseId)
            .OrderBy(i => i.Order).ThenBy(i => i.Id)
            .ToListAsync();

        var submissions = await db.Submissions
            .Where(s => s.Assignment.CourseId == courseId && s.Score != null)
            .ToListAsync();

        var attempts = await db.QuizAttempts
            .Where(a => a.Quiz.CourseId == courseId && a.SubmittedAt != null)
            .ToListAsync();

        var entries = await db.CourseGradeEntries
            .Where(e => e.GradeItem.CourseId == courseId)
            .ToListAsync();

        return enrollments.Select(e =>
        {
            var views = items.Select(item => BuildItemView(item, e.UserId,
                submissions.Where(s => s.UserId == e.UserId).ToList(),
                attempts.Where(a => a.UserId == e.UserId).ToList(),
                entries.Where(en => en.UserId == e.UserId).ToList())).ToList();

            var (avg, letter) = ComputeWeightedAverage(views);
            return new WeightedStudentRow(e.UserId, e.UserName, views, avg, letter);
        }).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WeightedGradeItemView BuildItemView(
        CourseGradeItem item,
        string userId,
        List<Submission> submissions,
        List<QuizAttempt> attempts,
        List<CourseGradeEntry> manualEntries)
    {
        double? score = null;
        string status = "not_submitted";

        switch (item.Type)
        {
            case GradeItemType.Assignment:
                var sub = submissions.FirstOrDefault(s => s.AssignmentId == item.AssignmentId);
                if (sub?.Score != null) { score = sub.Score; status = "graded"; }
                break;

            case GradeItemType.Quiz:
                var attempt = attempts
                    .Where(a => a.QuizId == item.QuizId)
                    .OrderByDescending(a => a.Score)
                    .FirstOrDefault();
                if (attempt != null) { score = attempt.Score; status = "graded"; }
                break;

            case GradeItemType.Manual:
                var entry = manualEntries.FirstOrDefault(e => e.GradeItemId == item.Id);
                if (entry != null) { score = entry.Score; status = "manual"; }
                break;
        }

        double? pct = score.HasValue ? score.Value / item.MaxScore * 100 : null;
        return new WeightedGradeItemView(
            item.Id, item.Name, item.Type.ToString(), item.Weight,
            item.MaxScore, score, pct, status);
    }

    private static (double? avg, string letter) ComputeWeightedAverage(List<WeightedGradeItemView> views)
    {
        var graded = views.Where(v => v.Score.HasValue).ToList();
        if (!graded.Any()) return (null, "-");

        double totalWeight = views.Sum(v => v.Weight);
        if (totalWeight == 0) return (null, "-");

        double weightedSum = graded.Sum(v => v.Percentage!.Value * v.Weight);
        double gradedWeight = graded.Sum(v => v.Weight);

        // Weighted average over ALL items (ungraded items count as 0 weight contribution
        // but reduce the denominator). We use total weight as denominator so ungraded
        // items implicitly pull down the average.
        double avg = weightedSum / totalWeight;
        return (Math.Round(avg, 1), ToLetterGrade(avg));
    }

    private static string ToLetterGrade(double pct) => pct switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "E"
    };

    private static GradeItemConfigDto ToConfigDto(CourseGradeItem i) => new(
        i.Id, i.CourseId, i.AssignmentId, i.QuizId,
        i.Name, i.Type.ToString(), i.MaxScore, i.Weight,
        i.Order, i.IsVisible, i.CreatedAt);
}
