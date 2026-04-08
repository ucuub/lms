using System.Security.Claims;

namespace LmsApp.Services;

/// <summary>
/// Development-only middleware: jika request membawa header X-Mock-User-Id,
/// bypass JWT dan buat ClaimsPrincipal langsung dari header tersebut.
/// Aktif HANYA saat ASPNETCORE_ENVIRONMENT=Development.
/// </summary>
public class MockAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.Request.Headers["X-Mock-User-Id"].ToString();

        if (!string.IsNullOrEmpty(userId))
        {
            var role = context.Request.Headers["X-Mock-User-Role"].FirstOrDefault() ?? "student";
            var name = context.Request.Headers["X-Mock-User-Name"].FirstOrDefault() ?? userId;

            var claims = new List<Claim>
            {
                new("sub",                userId),
                new("preferred_username", userId),
                new("name",               name),
                new("email",              $"{userId}@demo.local"),
                new("roles",              role),
                new("azp",                "lms-app"),
            };

            var identity = new ClaimsIdentity(claims, "Mock");
            context.User = new ClaimsPrincipal(identity);
        }

        await next(context);
    }
}
