using System.ComponentModel.DataAnnotations;

namespace LmsApp.DTOs;

// ── Response DTOs (tetap ada untuk /me dan /sync) ─────────────────────────────

public record UserDto(
    int Id,
    string UserId,
    string Name,
    string Email,
    string Role,
    string? AvatarUrl,
    bool IsActive,
    DateTime CreatedAt
);

// ── Request DTOs yang masih digunakan ────────────────────────────────────────

public record UpdateProfileRequest(
    [Required, MaxLength(100)] string Name
);
