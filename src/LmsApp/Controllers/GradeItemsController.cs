using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class GradeItemsController(LmsDbContext db, IGradebookService gradebook) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    // GET /api/courses/{courseId}/grade-items
    [HttpGet("courses/{courseId:int}/grade-items")]
    public async Task<ActionResult<List<GradeItemConfigDto>>> GetByCourse(int courseId)
    {
        if (!await CanAccess(courseId)) return Forbid();
        return Ok(await gradebook.GetGradeItemsAsync(courseId));
    }

    // POST /api/courses/{courseId}/grade-items
    [HttpPost("courses/{courseId:int}/grade-items")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<GradeItemConfigDto>> Create(int courseId, CreateGradeItemRequest req)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();
        var item = await gradebook.CreateGradeItemAsync(courseId, req, UserId);
        return CreatedAtAction(nameof(GetByCourse), new { courseId }, item);
    }

    // PUT /api/grade-items/{id}/weight
    [HttpPut("grade-items/{id:int}/weight")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<GradeItemConfigDto>> SetWeight(int id, SetWeightRequest req)
    {
        if (req.Weight < 0)
            return BadRequest(new { message = "Weight tidak boleh negatif." });

        var item = await db.CourseGradeItems.FindAsync(id);
        if (item == null) return NotFound();
        if (!await IsTeacherOrAdmin(item.CourseId)) return Forbid();

        var result = await gradebook.UpdateWeightAsync(id, req.Weight, UserId);
        return result == null ? NotFound() : Ok(result);
    }

    // POST /api/grade-items/{id}/grade  (manual grade or override)
    [HttpPost("grade-items/{id:int}/grade")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> SetGrade(int id, SetManualGradeRequest req)
    {
        var item = await db.CourseGradeItems.FindAsync(id);
        if (item == null) return NotFound();
        if (!await IsTeacherOrAdmin(item.CourseId)) return Forbid();

        if (req.Score < 0 || req.Score > item.MaxScore)
            return BadRequest(new { message = $"Score harus antara 0 dan {item.MaxScore}." });

        var entry = await gradebook.SetManualGradeAsync(id, req, UserId);
        return Ok(new { entry.Id, entry.GradeItemId, entry.UserId, entry.Score, entry.Comment, entry.GradedAt });
    }

    // DELETE /api/grade-items/{id}
    [HttpDelete("grade-items/{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.CourseGradeItems.FindAsync(id);
        if (item == null) return NotFound();
        if (!await IsTeacherOrAdmin(item.CourseId)) return Forbid();

        await gradebook.DeleteGradeItemAsync(id, UserId);
        return NoContent();
    }

    // GET /api/courses/{courseId}/grades  (student: weighted view)
    [HttpGet("courses/{courseId:int}/grades")]
    public async Task<ActionResult<WeightedGradebookView>> GetMyGrades(int courseId)
    {
        var isEnrolled = await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
        if (!isEnrolled && !await IsTeacherOrAdmin(courseId)) return Forbid();

        var view = await gradebook.GetStudentViewAsync(courseId, UserId);
        return view == null ? NotFound() : Ok(view);
    }

    // GET /api/courses/{courseId}/gradebook/weighted  (teacher: all students)
    [HttpGet("courses/{courseId:int}/gradebook/weighted")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<List<WeightedStudentRow>>> GetTeacherGradebook(int courseId)
    {
        if (!await IsTeacherOrAdmin(courseId)) return Forbid();
        return Ok(await gradebook.GetTeacherViewAsync(courseId));
    }

    private async Task<bool> CanAccess(int courseId)
    {
        if (await IsTeacherOrAdmin(courseId)) return true;
        return await db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == UserId);
    }

    private async Task<bool> IsTeacherOrAdmin(int courseId)
    {
        if (UserRole == "admin") return true;
        if (UserRole != "teacher") return false;
        return await db.Courses.AnyAsync(c => c.Id == courseId && c.InstructorId == UserId);
    }
}
