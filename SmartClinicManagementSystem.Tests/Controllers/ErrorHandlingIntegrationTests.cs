using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class ErrorHandlingIntegrationTests
    : IClassFixture<ControllerTestFactory>
{
    private readonly ControllerTestFactory _factory;

    public ErrorHandlingIntegrationTests(
        ControllerTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnknownApiRoute_ShouldReturnNotFoundWithoutExposingExceptionDetails()
    {
        using var client = CreateClient();

        var response =
            await client.GetAsync(
                "/api/does-not-exist");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "System.Exception",
            body,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "StackTrace",
            body,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownApiRoute_ShouldNotRedirectToLogin()
    {
        using var client = CreateClient();

        var response =
            await client.GetAsync(
                "/api/does-not-exist");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Null(response.Headers.Location);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
    }
}