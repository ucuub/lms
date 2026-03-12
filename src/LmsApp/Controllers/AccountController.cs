using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace LmsApp.Controllers;

public class AccountController : Controller
{
    public IActionResult Login(string? returnUrl = null)
    {
        var redirectUrl = Url.Action("Index", "Home");
        return Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl ?? redirectUrl },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            });

        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
