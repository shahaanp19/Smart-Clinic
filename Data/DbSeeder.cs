using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Data;

public static class DbSeeder
{
    private const string DefaultAdminEmail = "admin@smartclinic.local";
    private const string DefaultAdminPassword = "Admin@12345";

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        var passwordHasher = new PasswordHasher<User>();

        var existingAdmin = await context.Users
            .FirstOrDefaultAsync(u => u.Email == DefaultAdminEmail);

        if (existingAdmin is null)
        {
            var admin = new User
            {
                FullName = "System Administrator",
                Email = DefaultAdminEmail,
                Role = "Administrator",
                PhoneNumber = null,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null
            };

            admin.PasswordHash = passwordHasher.HashPassword(
                admin,
                DefaultAdminPassword);

            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();

            return;
        }

        if (!existingAdmin.IsActive)
        {
            existingAdmin.IsActive = true;
            existingAdmin.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}