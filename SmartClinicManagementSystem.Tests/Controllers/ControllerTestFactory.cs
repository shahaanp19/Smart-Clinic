using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class ControllerTestFactory : WebApplicationFactory<Program>
{
    private const string TestAdminEmail = "[admin@smartclinic.local](mailto:admin@smartclinic.local)";
    private const string TestAdminPassword = "Admin123!";


private readonly string _databaseName =
    $"SmartClinicManagementSystemIntegrationTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var existingDbContextOptions = services
                .Where(service =>
                    service.ServiceType ==
                    typeof(DbContextOptions<ApplicationDbContext>))
                .ToList();

            foreach (var descriptor in existingDbContextOptions)
            {
                services.Remove(descriptor);
            }

            var existingDbContext = services
                .Where(service =>
                    service.ServiceType ==
                    typeof(ApplicationDbContext))
                .ToList();

            foreach (var descriptor in existingDbContext)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseInMemoryDatabase(_databaseName));

            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.Database.EnsureCreated();

            var user = context.Users
                .FirstOrDefault(u => u.Email == TestAdminEmail);

            var passwordHasher = new PasswordHasher<User>();

            if (user is null)
            {
                user = new User
                {
                    FullName = "System Administrator",
                    Email = TestAdminEmail,
                    Role = "Administrator",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                user.PasswordHash =
                    passwordHasher.HashPassword(
                        user,
                        TestAdminPassword);

                context.Users.Add(user);
            }
            else
            {
                user.FullName = "System Administrator";
                user.Email = TestAdminEmail;
                user.Role = "Administrator";
                user.IsActive = true;
                user.PasswordHash =
                    passwordHasher.HashPassword(
                        user,
                        TestAdminPassword);
                user.UpdatedAtUtc = DateTime.UtcNow;
            }

            context.SaveChanges();
        });
    }


}
