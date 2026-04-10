using System.Security.Claims;

namespace LmsApp.Services;

/// <summary>
/// Development-only middleware: jika request membawa header X-Mock-User-Id,
/// bypass JWT dan buat ClaimsPrincipal langsung dari header tersebut.
/// Aktif HANYA saat ASPNETCORE_ENVIRONMENT=Development.
/// </summary>
public class MockAuthMiddleware(RequestDelegate next, ILogger<MockAuthMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.Request.Headers["X-Mock-User-Id"].ToString();
        var role   = context.Request.Headers["X-Mock-User-Role"].FirstOrDefault() ?? "student";
        var name   = context.Request.Headers["X-Mock-User-Name"].FirstOrDefault() ?? userId;

        logger.LogInformation("[MockAuth] Path={Path} UserId={UserId} Role={Role}",
            context.Request.Path, userId, userId == "" ? "(empty — no mock headers)" : role);

        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new("sub",                userId),
                new("preferred_username", userId),
                new("name",               name),
                new("email",              $"{userId}@demo.local"),
                new("roles",              role),
                new("role",               role),
                new("azp",                "lms-app"),
                new(ClaimTypes.Role,      role),
            };

            var identity = new ClaimsIdentity(claims, "Mock",
                nameType: "name",
                roleType: ClaimTypes.Role);
            context.User = new ClaimsPrincipal(identity);

            logger.LogInformation("[MockAuth] User set: {UserId} / {Role}", userId, role);
        }
        else
        {
            logger.LogWarning("[MockAuth] No X-Mock-User-Id header — request will be anonymous");
        }

        await next(context);
    }
}
