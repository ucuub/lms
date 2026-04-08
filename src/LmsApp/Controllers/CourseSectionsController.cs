using LmsApp.DTOs;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LmsApp.Controllers;

/// <summary>
/// Mengelola Section di dalam Course.
/// Route: api/courses/{courseId}/sections
///
/// Business Rules:
/// - Hanya instructor course atau admin yang bisa CREATE/UPDATE/DELETE/REORDER
/// - Student yang terdaftar bisa GET (hanya section IsVisible = true)
/// - Teacher course sendiri bisa GET semua section (termasuk IsVisible = false)
/// </summary>
[ApiController]
[Route("api/courses/{courseId:int}/sections")]
[Authorize]
public class CourseSectionsController(ICourseSectionService sectionService) : ControllerBase
{
    private string UserId => User.FindFirst("sub")?.Value ?? string.Empty;
    private bool IsAdmin => User.IsInRole("admin");
    private bool IsTeacher => User.IsInRole("teacher") || IsAdmin;

    // ── GET /api/courses/{courseId}/sections ──────────────────────────────────

    /// <summary>
    /// Ambil semua section beserta modul di dalamnya.
    /// - Teacher/Admin: melihat semua section (termasuk yang IsVisible=false)
    /// - Student/Guest: hanya section IsVisible=true dan module IsPublished=true
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SectionDetailDto>>> GetSections(int courseId)
    {
        var sections = await sectionService.GetSectionsAsync(courseId, includeHidden: IsTeacher);
        return Ok(sections);
    }

    // ── GET /api/courses/{courseId}/sections/{id} ─────────────────────────────

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<SectionDetailDto>> GetSection(int courseId, int id)
    {
        var section = await sectionService.GetByIdAsync(id, includeHidden: IsTeacher);
        if (section == null || section.CourseId != courseId)
            return NotFound(new { message = "Section tidak ditemukan." });

        return Ok(section);
    }

    // ── POST /api/courses/{courseId}/sections ─────────────────────────────────

    /// <summary>
    /// Buat section baru di dalam course.
    /// Contoh request body:
    /// {
    ///   "title": "Week 1: Introduction to HTML",
    ///   "description": "Materi dasar HTML dan struktur dokumen",
    ///   "order": 0,
    ///   "isVisible": true
    /// }
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<SectionDetailDto>> CreateSection(
        int courseId, CreateSectionRequest req)
    {
        try
        {
            var section = await sectionService.CreateAsync(courseId, req, UserId, IsAdmin);

            // Kembalikan DTO lengkap (section baru selalu kosong)
            var dto = new SectionDetailDto(
                section.Id, section.CourseId, section.Title, section.Description,
                section.Order, section.IsVisible, section.CreatedAt, []);

            return CreatedAtAction(nameof(GetSection), new { courseId, id = section.Id }, dto);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (UnauthorizedAccessException e)
        {
            return Forbid(e.Message);
        }
    }

    // ── PUT /api/courses/{courseId}/sections/{id} ─────────────────────────────

    /// <summary>
    /// Update judul, deskripsi, urutan, atau visibilitas section.
    /// Contoh: sembunyikan section dari student dengan isVisible: false.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> UpdateSection(
        int courseId, int id, UpdateSectionRequest req)
    {
        try
        {
            await sectionService.UpdateAsync(id, req, UserId, IsAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── DELETE /api/courses/{courseId}/sections/{id} ──────────────────────────

    /// <summary>
    /// Hapus section. PENTING: modul di dalam section TIDAK ikut terhapus.
    /// Modul tersebut menjadi "unsectioned" (SectionId = null).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> DeleteSection(int courseId, int id)
    {
        try
        {
            await sectionService.DeleteAsync(id, UserId, IsAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── PUT /api/courses/{courseId}/sections/reorder ──────────────────────────

    /// <summary>
    /// Atur ulang urutan section.
    /// Contoh request body:
    /// {
    ///   "items": [
    ///     { "id": 3, "order": 0 },
    ///     { "id": 1, "order": 1 },
    ///     { "id": 2, "order": 2 }
    ///   ]
    /// }
    /// </summary>
    [HttpPut("reorder")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> ReorderSections(
        int courseId, ReorderSectionsRequest req)
    {
        try
        {
            await sectionService.ReorderAsync(courseId, req.Items, UserId, IsAdmin);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── PATCH /api/courses/{courseId}/sections/modules/{moduleId}/move ────────

    /// <summary>
    /// Pindahkan modul ke section ini (atau keluar dari section dengan sectionId = null).
    /// Contoh: pindahkan modul 5 ke section 2
    ///   PATCH /api/courses/1/sections/modules/5/move
    ///   Body: { "sectionId": 2 }
    ///
    /// Contoh: keluarkan modul dari section (unsectioned)
    ///   Body: { "sectionId": null }
    /// </summary>
    [HttpPatch("modules/{moduleId:int}/move")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<IActionResult> MoveModule(
        int courseId, int moduleId, MoveModuleRequest req)
    {
        try
        {
            await sectionService.MoveModuleAsync(moduleId, req.SectionId, UserId, IsAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
