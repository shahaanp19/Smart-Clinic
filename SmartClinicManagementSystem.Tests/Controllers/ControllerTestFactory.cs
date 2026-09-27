using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartClinicManagementSystem.Data;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class ControllerTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"SmartClinicManagementSystemIntegrationTests_{Guid.NewGuid():N}";

    private string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;" +
        $"Database={_databaseName};" +
        "Trusted_Connection=True;" +
        "MultipleActiveResultSets=true;" +
        "TrustServerCertificate=True";

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
                    options.UseSqlServer(ConnectionString));

            using var serviceProvider =
                services.BuildServiceProvider();

            using var scope =
                serviceProvider.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            context.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                var options =
                    new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer(ConnectionString)
                        .Options;

                using var context =
                    new ApplicationDbContext(options);

                context.Database.EnsureDeleted();
            }
            catch
            {
                // Test cleanup must never hide the actual test result.
            }
        }

        base.Dispose(disposing);
    }
}