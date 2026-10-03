using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;

namespace SmartClinicManagementSystem.Services;

public class SmartClinicCookieAuthenticationEvents
    : CookieAuthenticationEvents
{
    private readonly ApplicationDbContext _context;

    public SmartClinicCookieAuthenticationEvents(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task ValidatePrincipal(
        CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            await RejectAsync(context);
            return;
        }

        var userIdValue =
            principal.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        var securityVersionValue =
            principal.FindFirst(
                "SecurityVersion")?.Value;

        if (!int.TryParse(
                userIdValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var userId))
        {
            await RejectAsync(context);
            return;
        }

        if (!DateTime.TryParse(
                securityVersionValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var cookieSecurityVersion))
        {
            await RejectAsync(context);
            return;
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.IsActive,
                u.Role,
                u.UpdatedAtUtc
            })
            .SingleOrDefaultAsync();

        if (user is null || !user.IsActive)
        {
            await RejectAsync(context);
            return;
        }

        var databaseSecurityVersion =
            user.UpdatedAtUtc ?? DateTime.MinValue;

        if (databaseSecurityVersion != cookieSecurityVersion)
        {
            await RejectAsync(context);
            return;
        }

        var cookieRole =
            principal.FindFirst(
                ClaimTypes.Role)?.Value;

        if (!string.Equals(
                cookieRole,
                user.Role,
                StringComparison.Ordinal))
        {
            await RejectAsync(context);
        }
    }

    private static async Task RejectAsync(
        CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}