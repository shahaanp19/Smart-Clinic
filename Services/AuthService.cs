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
            return false;

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                password);

            user.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    public async Task<bool> IsUserActiveAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = NormalizeEmail(email);

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Email == normalizedEmail &&
                u.IsActive);
    }

    public async Task<string?> GetUserRoleAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = NormalizeEmail(email);

        return await _context.Users
            .AsNoTracking()
            .Where(u =>
                u.Email == normalizedEmail &&
                u.IsActive)
            .Select(u => u.Role)
            .FirstOrDefaultAsync();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}