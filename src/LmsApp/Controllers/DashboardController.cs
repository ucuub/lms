using LmsApp.DTOs;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LmsApp.Controllers;

/// <summary>
/// Dashboard endpoint — returns role-appropriate dashboard in one call.
///
/// Routes:
///   GET api/dashboard          → auto-detect role (student | teacher | admin)
///   GET api/dashboard/student  → student dashboard (admin can impersonate)
///   GET api/dashboard/teacher  → teacher dashboard (admin can impersonate)
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    private string UserId   => User.FindFirst("sub")?.Value  ?? string.Empty;
    private string UserRole => User.FindFirst("role")?.Value ?? "student";

    /// <summary>
    /// GET api/dashboard
    /// Returns StudentDashboardDto for student role,
    /// TeacherDashboardDto for teacher/admin role.
    /// Response type varies by role — both are documented below.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (UserRole is "teacher" or "admin")
        {
            var result = await dashboardService.GetTeacherDashboardAsync(UserId);
            return Ok(result);
        }
        else
        {
            var result = await dashboardService.GetStudentDashboardAsync(UserId);
            return Ok(result);
        }
    }

    /// <summary>
    /// GET api/dashboard/student
    /// Explicitly request student dashboard.
    /// Admin can call this to preview the student view.
    /// </summary>
    [HttpGet("student")]
    public async Task<ActionResult<StudentDashboardDto>> GetStudent()
    {
        var result = await dashboardService.GetStudentDashboardAsync(UserId);
        return Ok(result);
    }

    /// <summary>
    /// GET api/dashboard/teacher
    /// Explicitly request teacher dashboard.
    /// </summary>
    [HttpGet("teacher")]
    [Authorize(Roles = "teacher,admin")]
    public async Task<ActionResult<TeacherDashboardDto>> GetTeacher()
    {
        var result = await dashboardService.GetTeacherDashboardAsync(UserId);
        return Ok(result);
    }
}
