using System.Security.Claims;
using LmsApp.Data;
using LmsApp.DTOs;
using LmsApp.Models;
using LmsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController(LmsDbContext db, IConfiguration config) : ControllerBase
{
    private string KeycloakId     => User.FindFirst("sub")?.Value ?? string.Empty;
    private string KeycloakClientId => config["Keycloak:ClientId"] ?? "lms-app";

    // ── Sync ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dipanggil frontend tepat setelah Keycloak login berhasil.
    /// Membuat record AppUser jika belum ada, lalu update data profil + role dari token.
    /// Role diambil dari Keycloak token (resource_access → realm_access).
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<UserDto>> Sync()
    {
        if (string.IsNullOrEmpty(KeycloakId))
            return Unauthorized(new { message = "Token tidak mengandung 'sub' claim." });

        var email = User.FindFirst(ClaimTypes.Email)?.Value
                 ?? User.FindFirst("email")?.Value
                 ?? string.Empty;
        var name  = User.FindFirst("name")?.Value
                 ?? User.FindFirst("preferred_username")?.Value
                 ?? string.Empty;

        // Ambil role dari Keycloak token — fallback ke claim "role" (mock auth), lalu "student"
        var role = KeycloakRoleExtractor.Extract(User, KeycloakClientId)
                ?? User.FindFirst("role")?.Value
                ?? "student";

        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == KeycloakId);

        if (user == null)
        {
            user = new AppUser
            {
                UserId      = KeycloakId,
                Name        = name,
                Email       = email,
                Role        = role,
                LastLoginAt = DateTime.UtcNow
            };
            db.AppUsers.Add(user);
        }
        else
        {
            user.Name        = name;
            user.Email       = email;
            user.Role        = role;   // Keycloak selalu jadi source of truth
            user.LastLoginAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Ok(ToDto(user));
    }

    // ── Profile ───────────────────────────────────────────────────────────────

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == KeycloakId);
        if (user == null)
            return NotFound(new { message = "User belum tersinkronisasi. Panggil POST /api/auth/sync terlebih dahulu." });

        return Ok(ToDto(user));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest req)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == KeycloakId);
        if (user == null) return NotFound();

        user.Name = req.Name;
        await db.SaveChangesAsync();
        return Ok(ToDto(user));
    }

    [HttpPost("avatar")]
    public async Task<ActionResult<UserDto>> UploadAvatar(IFormFile file, [FromServices] IFileUploadService fileService)
    {
        if (!fileService.IsValidFile(file, [".jpg", ".jpeg", ".png", ".webp"], 5 * 1024 * 1024))
            return BadRequest(new { message = "File tidak valid. Maks 5MB, format: jpg/png/webp." });

        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == KeycloakId);
        if (user == null) return NotFound();

        if (user.AvatarUrl != null) fileService.Delete(user.AvatarUrl);

        user.AvatarUrl = await fileService.UploadAsync(file, "avatars");
        await db.SaveChangesAsync();
        return Ok(ToDto(user));
    }

    // ── Debug (Development only) ──────────────────────────────────────────────

    [HttpGet("debug-claims")]
    public IActionResult DebugClaims([FromServices] IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var role = KeycloakRoleExtractor.Extract(User, KeycloakClientId);
        return Ok(new { extractedRole = role, clientId = KeycloakClientId, claims });
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static UserDto ToDto(AppUser u) =>
        new(u.Id, u.UserId, u.Name, u.Email, u.Role, u.AvatarUrl, u.IsActive, u.CreatedAt);
}
