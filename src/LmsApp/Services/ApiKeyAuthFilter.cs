using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LmsApp.Services;

/// <summary>
/// Action filter: validasi header X-Api-Key terhadap nilai di ServiceIntegration:ApiKey.
/// Tempel sebagai [ApiKeyAuth] di controller atau action yang perlu dilindungi.
/// Dipakai untuk service-to-service call (misalnya DWI Mobile → LMS).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config      = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config["ServiceIntegration:ApiKey"] ?? string.Empty;

        if (string.IsNullOrEmpty(expectedKey))
        {
            // API Key belum dikonfigurasi — tolak semua request sebagai failsafe
            context.Result = new ObjectResult(new { message = "Service integration belum dikonfigurasi." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var provided);

        if (string.IsNullOrWhiteSpace(provided) || provided != expectedKey)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "API Key tidak valid atau tidak diberikan." });
            return;
        }

        await next();
    }
}
