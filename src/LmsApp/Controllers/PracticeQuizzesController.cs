using LmsApp.DTOs;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LmsApp.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PracticeQuizzesController(IPracticeQuizService svc) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserName => User.FindFirst("name")?.Value
                            ?? User.FindFirst("preferred_username")?.Value
                            ?? "Unknown";
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    // ── Practice Quizzes ──────────────────────────────────────────────────────

    // GET /api/practice-quizzes  — semua user bisa lihat list
    [HttpGet("practice-quizzes")]
    public async Task<ActionResult<List<PracticeQuizDto>>> GetAll()
    {
        var result = await svc.GetAllAsync(UserId);
        return Ok(result);
    }

    // POST /api/practice-quizzes  — teacher/admin saja
    [HttpPost("practice-quizzes")]
    public async Task<ActionResult<PracticeQuizDto>> Create([FromBody] CreatePracticeQuizRequest req)
    {
        if (UserRole is not ("teacher" or "admin"))
            return Forbid();

        try
        {
            var result = await svc.CreateAsync(req, UserId, UserName);
            return CreatedAtAction(nameof(GetAll), new { }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE /api/practice-quizzes/{id}  — teacher/admin saja
    [HttpDelete("practice-quizzes/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (UserRole is not ("teacher" or "admin"))
            return Forbid();

        try
        {
            await svc.DeleteAsync(id, UserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Attempts ──────────────────────────────────────────────────────────────

    // POST /api/practice-quizzes/{id}/start  — semua user bisa mulai
    [HttpPost("practice-quizzes/{id:int}/start")]
    public async Task<ActionResult<StartPracticeResponse>> Start(int id)
    {
        try
        {
            var result = await svc.StartAttemptAsync(id, UserId, UserName);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/practice-attempts/{id}/submit
    [HttpPost("practice-attempts/{id:int}/submit")]
    public async Task<ActionResult<PracticeResultDto>> Submit(
        int id, [FromBody] SubmitPracticeRequest req)
    {
        try
        {
            var result = await svc.SubmitAttemptAsync(id, UserId, req);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/practice-attempts/me  — history attempt milik user
    [HttpGet("practice-attempts/me")]
    public async Task<ActionResult<List<PracticeAttemptSummaryDto>>> MyAttempts()
    {
        var result = await svc.GetMyAttemptsAsync(UserId);
        return Ok(result);
    }

    // GET /api/practice-attempts/{id}/result
    [HttpGet("practice-attempts/{id:int}/result")]
    public async Task<ActionResult<PracticeResultDto>> GetResult(int id)
    {
        try
        {
            var result = await svc.GetResultAsync(id, UserId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
