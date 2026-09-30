using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Models.ViewModels;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class UserService : IUserService
{
    private static readonly string[] AllowedRoles =
    {
        "Administrator",
        "Doctor",
        "Patient",
        "Receptionist"
    };

    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = NormalizeEmail(email);

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Email.ToLower() == normalizedEmail);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => new User
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PasswordHash = string.Empty,
                Role = u.Role,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
                UpdatedAtUtc = u.UpdatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        ValidateUser(user);
        NormalizeUser(user);

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Email.ToLower() == user.Email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email address already exists.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new InvalidOperationException(
                "A password is required.");
        }

        var plainTextPassword = user.PasswordHash;

        if (plainTextPassword.Length < 8)
        {
            throw new InvalidOperationException(
                "The password must contain at least 8 characters.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            plainTextPassword);

        user.IsActive = true;
        user.CreatedAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return user;
    }

    public async Task UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id <= 0)
        {
            throw new ArgumentException(
                "A valid user ID is required.",
                nameof(user));
        }

        ValidateUser(user);
        NormalizeUser(user);

        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (existing is null)
        {
            throw new KeyNotFoundException(
                "The user could not be found.");
        }

        var duplicateEmail = await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != user.Id &&
                u.Email.ToLower() == user.Email);

        if (duplicateEmail)
        {
            throw new InvalidOperationException(
                "A user with this email address already exists.");
        }

        if (existing.Role == "Administrator" &&
            existing.IsActive &&
            !string.Equals(
                user.Role,
                "Administrator",
                StringComparison.OrdinalIgnoreCase))
        {
            var activeAdministratorCount =
                await _context.Users.CountAsync(u =>
                    u.Role == "Administrator" &&
                    u.IsActive);

            if (activeAdministratorCount <= 1)
            {
                throw new InvalidOperationException(
                    "The last active administrator cannot be assigned another role.");
            }
        }

        if (existing.Role == "Administrator" &&
            existing.IsActive &&
            !user.IsActive)
        {
            var activeAdministratorCount =
                await _context.Users.CountAsync(u =>
                    u.Role == "Administrator" &&
                    u.IsActive);

            if (activeAdministratorCount <= 1)
            {
                throw new InvalidOperationException(
                    "The last active administrator cannot be deactivated.");
            }
        }

        existing.FullName = user.FullName;
        existing.Email = user.Email;
        existing.Role = user.Role;
        existing.PhoneNumber = user.PhoneNumber;
        existing.IsActive = user.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return false;
        }

        if (!user.IsActive)
        {
            return true;
        }

        if (user.Role == "Administrator")
        {
            var activeAdministratorCount =
                await _context.Users.CountAsync(u =>
                    u.Role == "Administrator" &&
                    u.IsActive);

            if (activeAdministratorCount <= 1)
            {
                throw new InvalidOperationException(
                    "The last active administrator cannot be deactivated.");
            }
        }

        user.IsActive = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    private static void ValidateUser(User user)
    {
        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            throw new InvalidOperationException(
                "Full name is required.");
        }

        if (user.FullName.Trim().Length > 100)
        {
            throw new InvalidOperationException(
                "Full name cannot exceed 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException(
                "Email address is required.");
        }

        if (user.Email.Trim().Length > 255)
        {
            throw new InvalidOperationException(
                "Email address cannot exceed 255 characters.");
        }

        if (string.IsNullOrWhiteSpace(user.Role))
        {
            throw new InvalidOperationException(
                "Role is required.");
        }

        if (!AllowedRoles.Contains(
                user.Role.Trim(),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The selected role is invalid.");
        }

        if (user.PhoneNumber is not null &&
            user.PhoneNumber.Trim().Length > 20)
        {
            throw new InvalidOperationException(
                "Phone number cannot exceed 20 characters.");
        }
    }

    private static void NormalizeUser(User user)
    {
        user.FullName = user.FullName.Trim();

        user.Email = NormalizeEmail(user.Email);

        user.Role = AllowedRoles.First(
            role => string.Equals(
                role,
                user.Role.Trim(),
                StringComparison.OrdinalIgnoreCase));

        user.PhoneNumber =
            string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? null
                : user.PhoneNumber.Trim();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}