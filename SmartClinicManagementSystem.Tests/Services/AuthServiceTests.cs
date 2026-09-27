using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Services;

public class AuthServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static User CreateUser(
        string email = "patient@example.com",
        string role = "Patient",
        bool isActive = true)
    {
        var user = new User
        {
            FullName = "Test Patient",
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            IsActive = isActive
        };

        var hasher = new PasswordHasher<User>();

        user.PasswordHash = hasher.HashPassword(
            user,
            "CorrectPassword123!");

        return user;
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnTrueForValidCredentials()
    {
        await using var context = CreateContext();

        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        var service = new AuthService(context);

        var result = await service.ValidateCredentialsAsync(
            "patient@example.com",
            "CorrectPassword123!");

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldIgnoreEmailCase()
    {
        await using var context = CreateContext();

        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        var service = new AuthService(context);

        var result = await service.ValidateCredentialsAsync(
            "PATIENT@EXAMPLE.COM",
            "CorrectPassword123!");

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnFalseForIncorrectPassword()
    {
        await using var context = CreateContext();

        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        var service = new AuthService(context);

        var result = await service.ValidateCredentialsAsync(
            "patient@example.com",
            "WrongPassword123!");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnFalseForUnknownUser()
    {
        await using var context = CreateContext();

        var service = new AuthService(context);

        var result = await service.ValidateCredentialsAsync(
            "unknown@example.com",
            "CorrectPassword123!");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnFalseForInactiveUser()
    {
        await using var context = CreateContext();

        context.Users.Add(
            CreateUser(isActive: false));

        await context.SaveChangesAsync();

        var service = new AuthService(context);

        var result = await service.ValidateCredentialsAsync(
            "patient@example.com",
            "CorrectPassword123!");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnFalseForEmptyCredentials()
    {
        await using var context = CreateContext();

        var service = new AuthService(context);

        Assert.False(
            await service.ValidateCredentialsAsync(
                string.Empty,
                "CorrectPassword123!"));

        Assert.False(
            await service.ValidateCredentialsAsync(
                "patient@example.com",
                string.Empty));
    }

    [Fact]
    public async Task IsUserActiveAsync_ShouldReturnTrueForActiveUser()
    {
        await using var context = CreateContext();

        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        var service = new AuthService(context);

        var result = await service.IsUserActiveAsync(
            "patient@example.com");

        Assert.True(result);
    }

    [Fact]
    public async Task IsUserActiveAsync_ShouldReturnFalseForInactiveUser()
    {
        await using var context = CreateContext();

        context.Users.Add(
            CreateUser(isActive: false));

        await context.SaveChangesAsync();

        var service = new AuthService(context);

        var result = await service.IsUserActiveAsync(
            "patient@example.com");

        Assert.False(result);
    }

    [Fact]
    public async Task IsUserActiveAsync_ShouldReturnFalseForUnknownUser()
    {
        await using var context = CreateContext();

        var service = new AuthService(context);

        var result = await service.IsUserActiveAsync(
            "unknown@example.com");

        Assert.False(result);
    }

    [Fact]
    public async Task GetUserRoleAsync_ShouldReturnUserRole()
    {
        await using var context = CreateContext();

        context.Users.Add(
            CreateUser(role: "Doctor"));

        await context.SaveChangesAsync();

        var service = new AuthService(context);

        var result = await service.GetUserRoleAsync(
            "patient@example.com");

        Assert.Equal("Doctor", result);
    }

    [Fact]
    public async Task GetUserRoleAsync_ShouldReturnNullForInactiveUser()
    {
        await using var context = CreateContext();

        context.Users.Add(
            CreateUser(isActive: false));

        await context.SaveChangesAsync();

        var service = new AuthService(context);

        var result = await service.GetUserRoleAsync(
            "patient@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserRoleAsync_ShouldReturnNullForUnknownUser()
    {
        await using var context = CreateContext();

        var service = new AuthService(context);

        var result = await service.GetUserRoleAsync(
            "unknown@example.com");

        Assert.Null(result);
    }
}

