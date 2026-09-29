using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<bool> ValidateCredentialsAsync(
        string email,
        string password)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var normalizedEmail = NormalizeEmail(email);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || !user.IsActive)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

        if (verificationResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash =
                _passwordHasher.HashPassword(user, password);

            user.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        return verificationResult == PasswordVerificationResult.Success ||
               verificationResult ==
               PasswordVerificationResult.SuccessRehashNeeded;
    }

    public async Task<string?> GetUserRoleAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = NormalizeEmail(email);

        return await _context.Users
            .AsNoTracking()
            .Where(u =>
                u.Email == normalizedEmail &&
                u.IsActive)
            .Select(u => u.Role)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetUserIdAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = NormalizeEmail(email);

        return await _context.Users
            .AsNoTracking()
            .Where(u =>
                u.Email == normalizedEmail &&
                u.IsActive)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsUserActiveAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalizedEmail = NormalizeEmail(email);

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Email == normalizedEmail &&
                u.IsActive);
    }

    public async Task<(int Id, string Role, DateTime UpdatedAtUtc)?>
        GetAuthenticationStateAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = NormalizeEmail(email);

        var user = await _context.Users
            .AsNoTracking()
            .Where(u =>
                u.Email == normalizedEmail &&
                u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.Role,
                u.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return null;
        }

        /*
         * UpdatedAtUtc may be null for older records.
         * Use DateTime.MinValue as the stable fallback value.
         */
        return (
            user.Id,
            user.Role,
            user.UpdatedAtUtc ?? DateTime.MinValue);
    }

    public async Task<bool> IsAuthenticationStateValidAsync(
        int userId,
        string role,
        string updatedAtUtc)
    {
        if (userId <= 0 ||
            string.IsNullOrWhiteSpace(role) ||
            string.IsNullOrWhiteSpace(updatedAtUtc))
        {
            return false;
        }

        if (!DateTime.TryParse(
                updatedAtUtc,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var cookieUpdatedAtUtc))
        {
            return false;
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(u =>
                u.Id == userId &&
                u.IsActive)
            .Select(u => new
            {
                u.Role,
                u.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return false;
        }

        var databaseUpdatedAtUtc =
            user.UpdatedAtUtc ?? DateTime.MinValue;

        return string.Equals(
                   user.Role,
                   role,
                   StringComparison.OrdinalIgnoreCase)
               &&
               databaseUpdatedAtUtc == cookieUpdatedAtUtc;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
