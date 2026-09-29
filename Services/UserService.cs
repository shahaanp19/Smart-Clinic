using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class UserService : IUserService
{
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

        var normalizedEmail = email.Trim();

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
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

        user.Email = user.Email.Trim();

        var existingUser = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == user.Email);

        if (existingUser)
        {
            throw new InvalidOperationException(
                "A user with this email address already exists.");
        }

        var now = DateTime.UtcNow;

        user.Id = 0;
        user.CreatedAtUtc = now;
        user.UpdatedAtUtc = now;

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                user.PasswordHash);
        }

        await _context.Users.AddAsync(user);
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

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (existingUser is null)
        {
            throw new KeyNotFoundException(
                $"User with ID {user.Id} was not found.");
        }

        var normalizedEmail = user.Email.Trim();

        var emailAlreadyExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id != user.Id &&
                u.Email == normalizedEmail);

        if (emailAlreadyExists)
        {
            throw new InvalidOperationException(
                "A user with this email address already exists.");
        }

        if (existingUser.Role == "Administrator" &&
            existingUser.IsActive &&
            !user.IsActive)
        {
            var activeAdministratorCount = await _context.Users
                .CountAsync(u =>
                    u.Role == "Administrator" &&
                    u.IsActive &&
                    u.Id != user.Id);

            if (activeAdministratorCount == 0)
            {
                throw new InvalidOperationException(
                    "The last active Administrator cannot be deactivated.");
            }
        }

        if (existingUser.Role == "Administrator" &&
            existingUser.IsActive &&
            !string.Equals(
                user.Role,
                "Administrator",
                StringComparison.OrdinalIgnoreCase))
        {
            var activeAdministratorCount = await _context.Users
                .CountAsync(u =>
                    u.Role == "Administrator" &&
                    u.IsActive &&
                    u.Id != user.Id);

            if (activeAdministratorCount == 0)
            {
                throw new InvalidOperationException(
                    "The last active Administrator cannot be changed to another role.");
            }
        }

        existingUser.FullName = user.FullName.Trim();
        existingUser.Email = normalizedEmail;
        existingUser.Role = user.Role.Trim();
        existingUser.PhoneNumber =
            string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? null
                : user.PhoneNumber.Trim();
        existingUser.IsActive = user.IsActive;
        existingUser.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            existingUser.PasswordHash =
                _passwordHasher.HashPassword(
                    existingUser,
                    user.PasswordHash);
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

        if (user.Role == "Administrator")
        {
            var activeAdministratorCount = await _context.Users
                .CountAsync(u =>
                    u.Role == "Administrator" &&
                    u.IsActive &&
                    u.Id != id);

            if (activeAdministratorCount == 0)
            {
                throw new InvalidOperationException(
                    "The last active Administrator cannot be deactivated.");
            }
        }

        user.IsActive = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}