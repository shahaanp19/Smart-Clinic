using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    private static readonly string[] AllowedRoles =
    {
        "Administrator",
        "Doctor",
        "Receptionist",
        "Patient"
    };

    public UserService(ApplicationDbContext context)
    {
        _context = context;
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
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == normalizedEmail);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        NormalizeUser(user);
        ValidateUser(user);

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Email.ToLower() == user.Email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email address already exists.");
        }

        user.CreatedAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = null;
        user.IsActive = true;

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return user;
    }

    public async Task UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id <= 0)
        {
            throw new InvalidOperationException(
                "A valid user is required.");
        }

        NormalizeUser(user);
        ValidateUser(user);

        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (existing is null)
        {
            throw new InvalidOperationException(
                "The user could not be found.");
        }

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != user.Id &&
                u.Email.ToLower() == user.Email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email address already exists.");
        }

        /*
         * Do not allow an update to deactivate the last administrator.
         */
        if (existing.IsActive &&
            !user.IsActive &&
            IsAdministrator(existing.Role))
        {
            var activeAdministrators = await _context.Users
                .CountAsync(u =>
                    u.IsActive &&
                    u.Role == "Administrator");

            if (activeAdministrators <= 1)
            {
                throw new InvalidOperationException(
                    "The last active administrator cannot be deactivated.");
            }
        }

        /*
         * Do not allow the last administrator to have their role changed
         * to a non-administrator role.
         */
        if (existing.IsActive &&
            IsAdministrator(existing.Role) &&
            !IsAdministrator(user.Role))
        {
            var activeAdministrators = await _context.Users
                .CountAsync(u =>
                    u.IsActive &&
                    u.Role == "Administrator");

            if (activeAdministrators <= 1)
            {
                throw new InvalidOperationException(
                    "The last active administrator cannot be assigned another role.");
            }
        }

        existing.FullName = user.FullName;
        existing.Email = user.Email;
        existing.Role = user.Role;
        existing.PhoneNumber = user.PhoneNumber;
        existing.IsActive = user.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        /*
         * PasswordHash is only replaced when a new hash was supplied.
         * This prevents ordinary profile/status updates from erasing a
         * user's existing password.
         */
        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            existing.PasswordHash = user.PasswordHash;
        }

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

        if (IsAdministrator(user.Role))
        {
            var activeAdministrators = await _context.Users
                .CountAsync(u =>
                    u.IsActive &&
                    u.Role == "Administrator");

            if (activeAdministrators <= 1)
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

    private static void NormalizeUser(User user)
    {
        user.FullName =
            user.FullName?.Trim() ?? string.Empty;

        user.Email =
            NormalizeEmail(user.Email);

        user.Role =
            NormalizeRole(user.Role);

        user.PhoneNumber =
            string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? null
                : user.PhoneNumber.Trim();

        user.PasswordHash =
            user.PasswordHash?.Trim() ?? string.Empty;
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return string.Empty;
        }

        var matchingRole = AllowedRoles.FirstOrDefault(r =>
            string.Equals(
                r,
                role.Trim(),
                StringComparison.OrdinalIgnoreCase));

        return matchingRole ?? role.Trim();
    }

    private static bool IsAdministrator(string? role)
    {
        return string.Equals(
            role,
            "Administrator",
            StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateUser(User user)
    {
        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            throw new InvalidOperationException(
                "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException(
                "Email address is required.");
        }

        if (!IsValidEmail(user.Email))
        {
            throw new InvalidOperationException(
                "A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(user.Role))
        {
            throw new InvalidOperationException(
                "A user role is required.");
        }

        if (!AllowedRoles.Any(role =>
                string.Equals(
                    role,
                    user.Role,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "The selected user role is invalid.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new InvalidOperationException(
                "A password hash is required.");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(email);

            return string.Equals(
                address.Address,
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
