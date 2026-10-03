using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Data;

public static class DbSeeder
{
    private const string DefaultAdminEmail = "admin@smartclinic.local";

    /*
     * The initial administrator password must never be stored in source
     * code or appsettings.json. It is supplied through application
     * configuration, normally via an environment variable or User Secrets.
     */
    private const string AdminPasswordConfigurationKey =
        "SeedAdmin:Password";

    public static async Task SeedAsync(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        var existingAdmin = await context.Users
            .FirstOrDefaultAsync(u => u.Email == DefaultAdminEmail);

        /*
         * Seeding is intentionally idempotent.
         *
         * If the administrator already exists, including when the
         * account is inactive, do nothing. The seeder must never
         * silently reactivate an account that an administrator
         * deliberately deactivated.
         */
        if (existingAdmin is not null)
        {
            return;
        }

        var initialAdminPassword =
            configuration[AdminPasswordConfigurationKey];

        if (string.IsNullOrWhiteSpace(initialAdminPassword))
        {
            throw new InvalidOperationException(
                $"The initial administrator password is not configured. " +
                $"Configure '{AdminPasswordConfigurationKey}' using a " +
                "secure secret source before starting the application.");
        }

        if (initialAdminPassword.Length < 8)
        {
            throw new InvalidOperationException(
                "The initial administrator password must contain at least 8 characters.");
        }

        var passwordHasher = new PasswordHasher<User>();

        var admin = new User
        {
            FullName = "System Administrator",
            Email = DefaultAdminEmail,
            Role = "Administrator",
            PhoneNumber = null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        admin.PasswordHash = passwordHasher.HashPassword(
            admin,
            initialAdminPassword);

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }
}

