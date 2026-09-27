using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Services;

public class UserServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(options);
    }

    private static User CreateUser(
        string fullName = "Test User",
        string email = "user@example.com",
        string role = "Patient",
        bool isActive = true,
        string passwordHash = "hashed-password")
    {
        return new User
        {
            FullName = fullName,
            Email = email,
            Role = role,
            PasswordHash = passwordHash,
            PhoneNumber = "0820000000",
            IsActive = isActive
        };
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user = CreateUser();

        var result =
            await service.CreateAsync(user);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(
            "Test User",
            result.FullName);
        Assert.Equal(
            "user@example.com",
            result.Email);
        Assert.Equal(
            "Patient",
            result.Role);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeUserFields()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user = CreateUser(
            fullName: "  Test User  ",
            email: "  USER@EXAMPLE.COM  ",
            role: "  doctor  ");

        user.PhoneNumber = " 0821234567 ";

        var result =
            await service.CreateAsync(user);

        Assert.Equal(
            "Test User",
            result.FullName);

        Assert.Equal(
            "user@example.com",
            result.Email);

        Assert.Equal(
            "Doctor",
            result.Role);

        Assert.Equal(
            "0821234567",
            result.PhoneNumber);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeBlankPhoneNumberToNull()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user = CreateUser();

        user.PhoneNumber = "   ";

        var result =
            await service.CreateAsync(user);

        Assert.Null(result.PhoneNumber);
    }

    [Fact]
    public async Task CreateAsync_ShouldForceUserActive()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(isActive: false);

        var result =
            await service.CreateAsync(user);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetCreatedTimestamp()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user = CreateUser();

        var before = DateTime.UtcNow;

        var result =
            await service.CreateAsync(user);

        var after = DateTime.UtcNow;

        Assert.InRange(
            result.CreatedAtUtc,
            before,
            after);

        Assert.Null(result.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNullUser()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingFullName()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(fullName: "   ");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(user));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingEmail()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(email: "   ");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(user));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("missing-at.example.com")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public async Task CreateAsync_ShouldRejectInvalidEmail(
        string email)
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(email: email);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(user));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingRole()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(role: "   ");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(user));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidRole()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(role: "Manager");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(user));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingPasswordHash()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(passwordHash: "   ");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(user));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateEmail()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        await service.CreateAsync(
            CreateUser(
                email: "duplicate@example.com"));

        var duplicate =
            CreateUser(
                fullName: "Second User",
                email: " DUPLICATE@EXAMPLE.COM ");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(duplicate));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result =
            await service.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(
            user.Id,
            result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        Assert.Null(
            await service.GetByIdAsync(0));

        Assert.Null(
            await service.GetByIdAsync(-1));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenUserDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        Assert.Null(
            await service.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldBeCaseInsensitive()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(
                email: "test@example.com");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result =
            await service.GetByEmailAsync(
                "  TEST@EXAMPLE.COM ");

        Assert.NotNull(result);
        Assert.Equal(
            user.Id,
            result.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullForBlankEmail()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        Assert.Null(
            await service.GetByEmailAsync("   "));
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullWhenUserDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        Assert.Null(
            await service.GetByEmailAsync(
                "missing@example.com"));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsersOrderedByFullName()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        context.Users.AddRange(
            CreateUser(
                fullName: "Zach User",
                email: "zach@example.com"),
            CreateUser(
                fullName: "Alice User",
                email: "alice@example.com"),
            CreateUser(
                fullName: "Michael User",
                email: "michael@example.com"));

        await context.SaveChangesAsync();

        var result =
            await service.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(
            "Alice User",
            result[0].FullName);
        Assert.Equal(
            "Michael User",
            result[1].FullName);
        Assert.Equal(
            "Zach User",
            result[2].FullName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(
                fullName: "Original User",
                email: "original@example.com");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.FullName = "Updated User";
        user.Email = "UPDATED@EXAMPLE.COM";
        user.PhoneNumber = "0831234567";
        user.Role = "Doctor";

        await service.UpdateAsync(user);

        var updated =
            await context.Users
                .FirstAsync(u => u.Id == user.Id);

        Assert.Equal(
            "Updated User",
            updated.FullName);

        Assert.Equal(
            "updated@example.com",
            updated.Email);

        Assert.Equal(
            "0831234567",
            updated.PhoneNumber);

        Assert.Equal(
            "Doctor",
            updated.Role);

        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ShouldAllowUpdateWithoutReplacingPassword()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(
                passwordHash: "original-hash");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.FullName = "Updated User";
        user.PasswordHash = string.Empty;

        await service.UpdateAsync(user);

        var updated =
            await context.Users
                .FirstAsync(u => u.Id == user.Id);

        Assert.Equal(
            "original-hash",
            updated.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplacePasswordWhenNewHashSupplied()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser(
                passwordHash: "original-hash");

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.PasswordHash =
            "new-password-hash";

        await service.UpdateAsync(user);

        var updated =
            await context.Users
                .FirstAsync(u => u.Id == user.Id);

        Assert.Equal(
            "new-password-hash",
            updated.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNormalizeFields()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.FullName = "  Updated User  ";
        user.Email = " UPDATED@EXAMPLE.COM ";
        user.Role = " receptionist ";
        user.PhoneNumber = " 0831111111 ";

        await service.UpdateAsync(user);

        var updated =
            await context.Users
                .FirstAsync(u => u.Id == user.Id);

        Assert.Equal(
            "Updated User",
            updated.FullName);

        Assert.Equal(
            "updated@example.com",
            updated.Email);

        Assert.Equal(
            "Receptionist",
            updated.Role);

        Assert.Equal(
            "0831111111",
            updated.PhoneNumber);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectNullUser()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectInvalidId()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser();

        user.Id = 0;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(user));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenUserDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var user =
            CreateUser();

        user.Id = 9999;

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(user));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectDuplicateEmail()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var first =
            CreateUser(
                fullName: "First User",
                email: "first@example.com");

        var second =
            CreateUser(
                fullName: "Second User",
                email: "second@example.com");

        context.Users.AddRange(first, second);
        await context.SaveChangesAsync();

        second.Email =
            " FIRST@EXAMPLE.COM ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(second));
    }

    [Fact]
    public async Task DeactivateAsync_ShouldDeactivateUser()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var administrator =
            CreateUser(
                fullName: "Admin User",
                email: "admin@example.com",
                role: "Administrator");

        var patient =
            CreateUser(
                fullName: "Patient User",
                email: "patient@example.com",
                role: "Patient");

        context.Users.AddRange(
            administrator,
            patient);

        await context.SaveChangesAsync();

        var result =
            await service.DeactivateAsync(
                patient.Id);

        Assert.True(result);

        var updated =
            await context.Users
                .FirstAsync(u => u.Id == patient.Id);

        Assert.False(updated.IsActive);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnFalseForInvalidId()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        Assert.False(
            await service.DeactivateAsync(0));

        Assert.False(
            await service.DeactivateAsync(-1));
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnFalseWhenUserDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        Assert.False(
            await service.DeactivateAsync(9999));
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnTrueWhenAlreadyInactive()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var administrator =
            CreateUser(
                fullName: "Admin User",
                email: "admin@example.com",
                role: "Administrator");

        var patient =
            CreateUser(
                fullName: "Patient User",
                email: "patient@example.com",
                role: "Patient",
                isActive: false);

        context.Users.AddRange(
            administrator,
            patient);

        await context.SaveChangesAsync();

        Assert.True(
            await service.DeactivateAsync(patient.Id));
    }

    [Fact]
    public async Task DeactivateAsync_ShouldPreventLastAdministratorDeactivation()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var administrator =
            CreateUser(
                fullName: "Admin User",
                email: "admin@example.com",
                role: "Administrator");

        context.Users.Add(administrator);
        await context.SaveChangesAsync();

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.DeactivateAsync(
                    administrator.Id));

        Assert.Contains(
            "last active administrator",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        var unchanged =
            await context.Users
                .FirstAsync(u =>
                    u.Id == administrator.Id);

        Assert.True(unchanged.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventLastAdministratorDeactivation()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var administrator =
            CreateUser(
                fullName: "Admin User",
                email: "admin@example.com",
                role: "Administrator");

        context.Users.Add(administrator);
        await context.SaveChangesAsync();

        administrator.IsActive = false;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(administrator));

        var unchanged =
            await context.Users
                .AsNoTracking()
                .FirstAsync(u =>
                    u.Id == administrator.Id);

        Assert.True(unchanged.IsActive);
        Assert.Equal(
            "Administrator",
            unchanged.Role);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventLastAdministratorRoleChange()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var administrator =
            CreateUser(
                fullName: "Admin User",
                email: "admin@example.com",
                role: "Administrator");

        context.Users.Add(administrator);
        await context.SaveChangesAsync();

        administrator.Role = "Doctor";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(administrator));

        var unchanged =
            await context.Users
                .AsNoTracking()
                .FirstAsync(u =>
                    u.Id == administrator.Id);

        Assert.Equal(
            "Administrator",
            unchanged.Role);

        Assert.True(unchanged.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_ShouldAllowAdministratorRoleChangeWhenAnotherAdministratorExists()
    {
        await using var context = CreateContext();

        var service = new UserService(context);

        var firstAdministrator =
            CreateUser(
                fullName: "First Admin",
                email: "firstadmin@example.com",
                role: "Administrator");

        var secondAdministrator =
            CreateUser(
                fullName: "Second Admin",
                email: "secondadmin@example.com",
                role: "Administrator");

        context.Users.AddRange(
            firstAdministrator,
            secondAdministrator);

        await context.SaveChangesAsync();

        firstAdministrator.Role = "Doctor";

        await service.UpdateAsync(
            firstAdministrator);

        var updated =
            await context.Users
                .FirstAsync(u =>
                    u.Id == firstAdministrator.Id);

        Assert.Equal(
            "Doctor",
            updated.Role);

        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_ShouldAllowAdministratorDeactivationWhenAnotherAdministratorExists()
    {
        await using var context = CreateContext();

        var firstAdministrator =
            CreateUser(
                fullName: "First Admin",
                email: "firstadmin@example.com",
                role: "Administrator");

        var secondAdministrator =
            CreateUser(
                fullName: "Second Admin",
                email: "secondadmin@example.com",
                role: "Administrator");

        context.Users.AddRange(
            firstAdministrator,
            secondAdministrator);

        await context.SaveChangesAsync();

        var service = new UserService(context);

        var result =
            await service.DeactivateAsync(
                firstAdministrator.Id);

        Assert.True(result);

        var updated =
            await context.Users
                .FirstAsync(u =>
                    u.Id == firstAdministrator.Id);

        Assert.False(updated.IsActive);

        var remaining =
            await context.Users
                .FirstAsync(u =>
                    u.Id == secondAdministrator.Id);

        Assert.True(remaining.IsActive);
    }
}