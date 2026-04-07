using System.Security.Claims;
using LmsApp.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace LmsApp.Services;

/// <summary>
/// Dijalankan pada setiap authenticated request.
///
/// Alur:
///   1. Ambil role dari Keycloak token (resource_access → realm_access)
///   2. Jika role berubah dari yang tersimpan di DB → update DB
///   3. Inject role ke ClaimsPrincipal agar [Authorize(Roles="...")] bekerja
///
/// Keycloak adalah source of truth untuk role.
/// DB menyimpan role sebagai cache — digunakan juga oleh gradebook, dll.
/// </summary>
public class AppUserClaimsTransformation(LmsDbContext db, IConfiguration config) : IClaimsTransformation
{
    private string KeycloakClientId => config["Keycloak:ClientId"] ?? "lms-app";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var keycloakId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(keycloakId)) return principal;

        var user = await db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == keycloakId);

        if (user == null) return principal;   // belum sync — panggil /api/auth/sync
        if (!user.IsActive) return principal; // akun dinonaktifkan

        // ── Ekstrak role dari Keycloak token ──────────────────────────────────
        var tokenRole = KeycloakRoleExtractor.Extract(principal, KeycloakClientId);

        // ── Sync role ke DB jika berubah ──────────────────────────────────────
        // AsNoTracking dipakai, jadi update dilakukan via ExecuteUpdateAsync
        // (targeted UPDATE tanpa load entity penuh)
        if (tokenRole != null && tokenRole != user.Role)
        {
            await db.AppUsers
                .Where(u => u.UserId == keycloakId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, tokenRole));
        }

        var effectiveRole = tokenRole ?? user.Role;

        // ── Build ClaimsPrincipal baru dengan role dari LMS ───────────────────
        var identity = new ClaimsIdentity(principal.Identity);

        // Hapus role lama (dari Keycloak) agar tidak bentrok
        foreach (var r in identity.FindAll(ClaimTypes.Role).ToList())
            identity.RemoveClaim(r);
        foreach (var r in identity.FindAll("roles").ToList())
            identity.RemoveClaim(r);

        // Inject role LMS — ini yang dibaca oleh [Authorize(Roles="...")]
        identity.AddClaim(new Claim(ClaimTypes.Role, effectiveRole));

        // Shorthand claims yang dipakai di seluruh controller
        // User.FindFirst("role")?.Value  → role LMS
        // User.FindFirst("userId")?.Value → Keycloak sub
        identity.AddClaim(new Claim("role",   effectiveRole));
        identity.AddClaim(new Claim("userId", user.UserId));
        identity.AddClaim(new Claim("name",   user.Name));

        return new ClaimsPrincipal(identity);
    }
}
