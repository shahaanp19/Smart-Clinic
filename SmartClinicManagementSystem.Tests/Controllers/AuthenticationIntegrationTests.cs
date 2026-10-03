using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class AuthenticationIntegrationTests
    : IClassFixture<ControllerTestFactory>
{
    private const string AdminEmail = "admin@smartclinic.local";
    private const string AdminPassword = "Password123";

    private readonly ControllerTestFactory _factory;

    public AuthenticationIntegrationTests(
        ControllerTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ValidAdminCredentials_ShouldAuthenticateAndRedirectToAdminDashboard()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                ("email", AdminEmail),
                ("password", AdminPassword)));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task InvalidPassword_ShouldNotAuthenticate()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                ("email", AdminEmail),
                ("password", "WrongPassword@999")));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Account/Login",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task UnknownUser_ShouldNotAuthenticate()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                (
                    "email",
                    $"unknown.{Guid.NewGuid():N}@test.local"),
                ("password", "Password@123")));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Account/Login",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task InactiveUser_ShouldNotAuthenticate()
    {
        var email =
            $"inactive.{Guid.NewGuid():N}@test.local";

        using (var scope = _factory.Services.CreateScope())
        {
            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var userService =
                scope.ServiceProvider
                    .GetRequiredService<IUserService>();

            var user = new User
            {
                FullName = "Inactive Authentication User",
                Email = email,
                PasswordHash = new PasswordHasher<User>()
                    .HashPassword(
                        null!,
                        "Password@123"),
                Role = "Doctor",
                PhoneNumber = "0110000099",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await userService.CreateAsync(user);

            var persistedUser =
                await context.Users.FindAsync(user.Id);

            Assert.NotNull(persistedUser);

            persistedUser!.IsActive = false;

            await context.SaveChangesAsync();
        }

        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                ("email", email),
                ("password", "Password@123")));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Account/Login",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task Login_ShouldNormalizeEmailWhitespaceAndCase()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                (
                    "email",
                    $"  {AdminEmail.ToUpperInvariant()}  "),
                ("password", AdminPassword)));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ValidLogin_ShouldCreateAuthenticatedSession()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var loginResponse = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                ("email", AdminEmail),
                ("password", AdminPassword)));

        Assert.Equal(
            "/Admin/Dashboard",
            loginResponse.RequestMessage?.RequestUri?.AbsolutePath);

        var dashboardResponse =
            await client.GetAsync("/Admin/Dashboard");

        Assert.Equal(
            HttpStatusCode.OK,
            dashboardResponse.StatusCode);
    }

    [Fact]
    public async Task LocalReturnUrl_ShouldBeHonouredAfterLogin()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                ("email", AdminEmail),
                ("password", AdminPassword),
                ("returnUrl", "/Admin/Dashboard")));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ExternalReturnUrl_ShouldNotBeRedirectedTo()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                ("email", AdminEmail),
                ("password", AdminPassword),
                ("returnUrl", "https://evil.example.com")));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Admin/Dashboard",
            response.RequestMessage?.RequestUri?.AbsolutePath);

        Assert.DoesNotContain(
            "evil.example.com",
            response.RequestMessage?.RequestUri?.ToString()
                ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginWithEmptyEmail_ShouldReturnValidationFailure()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                ("email", ""),
                ("password", AdminPassword)));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Account/Login",
            response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task LoginWithEmptyPassword_ShouldReturnValidationFailure()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/Account/Login");

        var response = await client.PostAsync(
            "/Account/Login",
            CreateForm(
                token,
                ("email", AdminEmail),
                ("password", "")));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "/Account/Login",
            response.RequestMessage?.RequestUri?.AbsolutePath);
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

    private static async Task<string> GetAntiforgeryTokenAsync(
        HttpClient client,
        string path)
    {
        var response =
            await client.GetAsync(path);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        return ExtractAntiforgeryToken(
            await response.Content.ReadAsStringAsync());
    }

    private static string ExtractAntiforgeryToken(
        string html)
    {
        var patterns = new[]
        {
            """
            <input[^>]*name\s*=\s*["']__RequestVerificationToken["'][^>]*value\s*=\s*["']([^"']+)["']
            """,
            """
            <input[^>]*value\s*=\s*["']([^"']+)["'][^>]*name\s*=\s*["']__RequestVerificationToken["']
            """
        };

        foreach (var pattern in patterns)
        {
            var match =
                Regex.Match(
                    html,
                    pattern,
                    RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        Assert.Fail(
            "The page did not contain an antiforgery token.");

        return string.Empty;
    }

    private static FormUrlEncodedContent CreateForm(
        string antiforgeryToken,
        params (string Name, string Value)[] values)
    {
        var fields =
            values
                .Select(value =>
                    new KeyValuePair<string, string>(
                        value.Name,
                        value.Value))
                .ToList();

        fields.Add(
            new KeyValuePair<string, string>(
                "__RequestVerificationToken",
                antiforgeryToken));

        return new FormUrlEncodedContent(fields);
    }
}