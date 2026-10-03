using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartClinicManagementSystem.Data;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class ControllerTestFactory : WebApplicationFactory<Program>
{
    private const string TestAdminPassword = "Password123";

    private readonly string _databaseName =
        $"SmartClinicManagementSystemIntegrationTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["SeedAdmin:Password"] = TestAdminPassword
                    });
            });

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
        });
    }
}