using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class AuthenticationIntegrationTests
{
    private const string AdminEmail = "[admin@smartclinic.local](mailto:admin@smartclinic.local)";
    private const string AdminPassword = "Admin123!";


private readonly ControllerTestFactory _factory;

    public AuthenticationIntegrationTests()
    {
        _factory = new ControllerTestFactory();
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
                BaseAddress = new Uri("https://localhost")
            });
    }

    private static async Task<string> GetRequestVerificationTokenAsync(
        HttpClient client)
    {
        var response = await client.GetAsync("/Account/Login");

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

        Assert.True(
            match.Success,
            "The login page did not contain an antiforgery token.");

        return match.Groups[1].Value;
    }

    private static FormUrlEncodedContent CreateLoginForm(
        string email,
        string password,
        string token,
        string? returnUrl = null)
    {
        var values = new List<KeyValuePair<string, string>>
    {
        new("Email", email),
        new("Password", password),
        new("__RequestVerificationToken", token)
    };

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            values.Add(new("returnUrl", returnUrl));
        }

        return new FormUrlEncodedContent(values);
    }

    [Fact]
    public async Task ValidLogin_ShouldCreateAuthenticatedSession()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            AdminEmail,
            AdminPassword,
            token);

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ValidAdminCredentials_ShouldAuthenticateAndRedirectToAdminDashboard()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            AdminEmail,
            AdminPassword,
            token);

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task Login_ShouldNormalizeEmailWhitespaceAndCase()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            $"  {AdminEmail.ToUpperInvariant()}  ",
            AdminPassword,
            token);

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task LocalReturnUrl_ShouldBeHonouredAfterLogin()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            AdminEmail,
            AdminPassword,
            token,
            "/Admin/Reports");

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            "/Admin/Reports",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ExternalReturnUrl_ShouldNotBeRedirectedTo()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            AdminEmail,
            AdminPassword,
            token,
            "https://example.com/");

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task InvalidPassword_ShouldNotAuthenticate()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            AdminEmail,
            "WrongPassword123!",
            token);

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            "/Account/Login",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task UnknownEmail_ShouldNotAuthenticate()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            "unknown@smartclinic.local",
            AdminPassword,
            token);

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            "/Account/Login",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task InactiveUser_ShouldNotAuthenticate()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            AdminEmail,
            AdminPassword,
            token);

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task EmptyCredentials_ShouldNotAuthenticate()
    {
        using var client = CreateClient();

        var token = await GetRequestVerificationTokenAsync(client);

        using var content = CreateLoginForm(
            string.Empty,
            string.Empty,
            token);

        var response = await client.PostAsync(
            "/Account/Login",
            content);

        Assert.Equal(
            "/Account/Login",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }


}
