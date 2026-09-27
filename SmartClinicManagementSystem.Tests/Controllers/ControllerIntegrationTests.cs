using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class ControllerIntegrationTests
    : IClassFixture<ControllerTestFactory>
{
    private readonly ControllerTestFactory _factory;

    public ControllerIntegrationTests(
        ControllerTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousRequest_ToAdminDashboard_IsProtected()
    {
        using var client = CreateUnauthenticatedClient();

        var response =
            await client.GetAsync("/Admin/Dashboard");

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.ToString()
                ?? string.Empty);
    }

    [Fact]
    public async Task AnonymousRequest_ToDoctorDashboard_IsProtected()
    {
        using var client = CreateUnauthenticatedClient();

        var response =
            await client.GetAsync("/Doctor/Dashboard");

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.ToString()
                ?? string.Empty);
    }

    [Fact]
    public async Task AnonymousRequest_ToPatientDashboard_IsProtected()
    {
        using var client = CreateUnauthenticatedClient();

        var response =
            await client.GetAsync("/Patient/Dashboard");

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.ToString()
                ?? string.Empty);
    }

    [Fact]
    public async Task AnonymousRequest_ToReceptionDashboard_IsProtected()
    {
        using var client = CreateUnauthenticatedClient();

        var response =
            await client.GetAsync("/Reception/Dashboard");

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.ToString()
                ?? string.Empty);
    }

    [Fact]
    public async Task AnonymousRequest_ToAccountLogin_IsAllowed()
    {
        using var client = CreateClient();

        var response =
            await client.GetAsync("/Account/Login");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task AnonymousRequest_ToAdminLogin_IsAllowed()
    {
        using var client = CreateClient();

        var response =
            await client.GetAsync("/Admin/Login");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });
    }

    private HttpClient CreateUnauthenticatedClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }
}