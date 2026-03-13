using LmsApp.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LmsApp.Services;

/// <summary>
/// Merges local AppUser.Role into the authenticated user's claims,
/// so User.IsInRole() works with roles stored in our own database.
/// </summary>
public class AppUserClaimsTransformation(LmsDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) return principal;

        var appUser = await db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (appUser is null || !appUser.IsActive) return principal;

        // Only mutate when the role isn't already present (avoid duplicates on multiple calls)
        if (!principal.IsInRole(appUser.Role))
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(ClaimTypes.Role, appUser.Role));
            principal.AddIdentity(identity);
        }

        return principal;
    }
}
