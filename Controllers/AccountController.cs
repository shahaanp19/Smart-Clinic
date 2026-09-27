using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToRoleDashboard(User);
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string email,
        string password,
        string? returnUrl = null)
    {
        email = email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(
                nameof(email),
                "Email address is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(
                nameof(password),
                "Password is required.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var validCredentials =
            await _authService.ValidateCredentialsAsync(
                email,
                password);

        if (!validCredentials)
        {
            ModelState.AddModelError(
                string.Empty,
                "The email address or password is incorrect.");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var role =
            await _authService.GetUserRoleAsync(email);

        if (string.IsNullOrWhiteSpace(role))
        {
            ModelState.AddModelError(
                string.Empty,
                "Your account could not be loaded. Please contact an administrator.");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authenticationProperties =
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties);

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToRoleDashboard(principal);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["SuccessMessage"] =
            "You have been signed out successfully.";

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToRoleDashboard(
        ClaimsPrincipal principal)
    {
        if (principal.IsInRole("Administrator"))
        {
            return RedirectToAction(
                "Dashboard",
                "Admin");
        }

        if (principal.IsInRole("Doctor"))
        {
            return RedirectToAction(
                "Dashboard",
                "Doctor");
        }

        if (principal.IsInRole("Receptionist"))
        {
            return RedirectToAction(
                "Dashboard",
                "Reception");
        }

        if (principal.IsInRole("Patient"))
        {
            return RedirectToAction(
                "Dashboard",
                "Patient");
        }

        return RedirectToAction(
            "Index",
            "Home");
    }
}