using LmsApp.DTOs;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LmsApp.Controllers;

/// <summary>
/// Course Prerequisites — kelola syarat enrollment antar course.
///
/// Routes:
///   GET    api/courses/{courseId}/prerequisites          → list prerequisite course
///   GET    api/courses/{courseId}/prerequisites/check    → cek apakah user memenuhi syarat
///   POST   api/courses/{courseId}/prerequisites          → tambah prerequisite (instructor/admin)
///   DELETE api/courses/{courseId}/prerequisites/{id}     → hapus prerequisite (instructor/admin)
/// </summary>
[ApiController]
[Route("api/courses/{courseId:int}/prerequisites")]
[Authorize]
public class PrerequisitesController(IPrerequisiteService service) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";
    private bool   IsAdmin  => UserRole == "admin";

    // ── List ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/courses/{courseId}/prerequisites
    /// Menampilkan semua prerequisite beserta status apakah user sudah memenuhinya.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrerequisiteDto>>> GetAll(int courseId)
    {
        var result = await service.GetAsync(courseId, UserId);
        return Ok(result);
    }

    // ── Check ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/courses/{courseId}/prerequisites/check
    /// Cek apakah user saat ini sudah memenuhi semua prerequisite untuk enroll.
    /// Response: { "canEnroll": true, "prerequisites": [...] }
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<PrerequisiteCheckDto>> Check(int courseId)
    {
        var result = await service.CheckAsync(courseId, UserId);
        return Ok(result);
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST api/courses/{courseId}/prerequisites
    /// Body: { "prerequisiteCourseId": 2 }
    /// Hanya instructor course atau admin.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<PrerequisiteDto>> Add(
        int courseId, AddPrerequisiteRequest req)
    {
        try
        {
            var result = await service.AddAsync(courseId, req.PrerequisiteCourseId, UserId, IsAdmin);
            return CreatedAtAction(nameof(GetAll), new { courseId }, result);
        }
        catch (KeyNotFoundException e)        { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException)   { return Forbid(); }
        catch (InvalidOperationException e)   { return BadRequest(new { message = e.Message }); }
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    /// <summary>
    /// DELETE api/courses/{courseId}/prerequisites/{id}
    /// Hanya instructor course atau admin.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> Remove(int courseId, int id)
    {
        try
        {
            await service.RemoveAsync(id, courseId, UserId, IsAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException e)        { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException)   { return Forbid(); }
    }
}
