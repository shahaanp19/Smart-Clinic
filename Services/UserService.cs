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
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
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
        ValidateUser(user, requirePasswordHash: true);

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == user.Email);

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
            throw new ArgumentException(
                "A valid user ID is required.",
                nameof(user));
        }

        /*
         * Always read the persisted state independently from the
         * caller's entity. This prevents EF Core tracking from
         * confusing requested changes with the existing database state.
         */
        var persistedUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (persistedUser is null)
        {
            throw new KeyNotFoundException(
                "The user could not be found.");
        }

        NormalizeUser(user);
        ValidateUser(user, requirePasswordHash: false);

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != user.Id &&
                u.Email == user.Email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email address already exists.");
        }

        /*
         * Protect the final active administrator using the persisted
         * role/status rather than potentially mutated caller values.
         */
        if (persistedUser.IsActive &&
            !user.IsActive &&
            IsAdministrator(persistedUser.Role))
        {
            await EnsureAnotherActiveAdministratorExistsAsync(
                persistedUser.Id);
        }

        if (persistedUser.IsActive &&
            IsAdministrator(persistedUser.Role) &&
            !IsAdministrator(user.Role))
        {
            await EnsureAnotherActiveAdministratorExistsAsync(
                persistedUser.Id);
        }

        /*
         * Reuse an already tracked entity when one exists.
         * Otherwise attach a new entity containing the persisted state.
         */
        var existing = _context.Users.Local
            .FirstOrDefault(u => u.Id == user.Id);

        if (existing is null)
        {
            existing = new User
            {
                Id = persistedUser.Id,
                FullName = persistedUser.FullName,
                Email = persistedUser.Email,
                PasswordHash = persistedUser.PasswordHash,
                Role = persistedUser.Role,
                PhoneNumber = persistedUser.PhoneNumber,
                IsActive = persistedUser.IsActive,
                CreatedAtUtc = persistedUser.CreatedAtUtc,
                UpdatedAtUtc = persistedUser.UpdatedAtUtc
            };

            _context.Users.Attach(existing);
        }

        existing.FullName = user.FullName;
        existing.Email = user.Email;
        existing.Role = user.Role;
        existing.PhoneNumber = user.PhoneNumber;
        existing.IsActive = user.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        /*
         * An empty password hash means that the existing password
         * must be retained. A supplied hash replaces it.
         */
        existing.PasswordHash =
            string.IsNullOrWhiteSpace(user.PasswordHash)
                ? persistedUser.PasswordHash
                : user.PasswordHash;

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
            await EnsureAnotherActiveAdministratorExistsAsync(
                user.Id);
        }

        user.IsActive = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task EnsureAnotherActiveAdministratorExistsAsync(
        int excludedUserId)
    {
        var anotherActiveAdministratorExists =
            await _context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id != excludedUserId &&
                    u.IsActive &&
                    u.Role == "Administrator");

        if (!anotherActiveAdministratorExists)
        {
            throw new InvalidOperationException(
                "The last active administrator cannot be deactivated or reassigned.");
        }
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

    private static void ValidateUser(
        User user,
        bool requirePasswordHash)
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

        if (requirePasswordHash &&
            string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new InvalidOperationException(
                "A password hash is required.");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address =
                new System.Net.Mail.MailAddress(email);

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