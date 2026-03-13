using LmsApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LmsApp.Controllers;

public class AccountController(IUserSyncService userSync) : Controller
{
    public IActionResult Login(string? returnUrl = null)
    {
        var redirectUrl = Url.Action("PostLogin", "Account");
        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUrl },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    // Called after OIDC redirect — sync user then go to returnUrl/Home
    [Authorize]
    public async Task<IActionResult> PostLogin(string? returnUrl = null)
    {
        var userId = User.FindFirst("sub")?.Value ?? string.Empty;
        var name = User.FindFirst("name")?.Value
                ?? User.FindFirst("preferred_username")?.Value
                ?? User.Identity?.Name ?? "Unknown";
        var email = User.FindFirst("email")?.Value ?? string.Empty;

        await userSync.SyncAsync(userId, name, email);

        return Redirect(returnUrl ?? Url.Action("Index", "Home")!);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") });

        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult AccessDenied() => View();
}
