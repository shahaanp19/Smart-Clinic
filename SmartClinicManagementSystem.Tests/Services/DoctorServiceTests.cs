using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Services;

public class DoctorServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Doctor CreateDoctor(
        string fullName = "Dr. John Smith",
        string employeeNumber = "DOC-001",
        string email = "john@example.com",
        bool isActive = true)
    {
        return new Doctor
        {
            FullName = fullName,
            EmployeeNumber = employeeNumber,
            Specialisation = "General Practitioner",
            Email = email,
            PhoneNumber = "0821234567",
            Qualifications = "MBChB",
            IsActive = isActive
        };
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndNormalizeDoctor()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        var doctor = new Doctor
        {
            FullName = "  Dr. John Smith  ",
            EmployeeNumber = " doc-001 ",
            Specialisation = " General Practitioner ",
            Email = " JOHN@EXAMPLE.COM ",
            PhoneNumber = " 0821234567 ",
            Qualifications = " MBChB "
        };

        var before = DateTime.UtcNow;

        var result = await service.CreateAsync(doctor);

        var after = DateTime.UtcNow;

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Dr. John Smith", result.FullName);
        Assert.Equal("DOC-001", result.EmployeeNumber);
        Assert.Equal(
            "General Practitioner",
            result.Specialisation);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("0821234567", result.PhoneNumber);
        Assert.Equal("MBChB", result.Qualifications);
        Assert.True(result.IsActive);
        Assert.InRange(result.CreatedAtUtc, before, after);
        Assert.Null(result.UpdatedAtUtc);

        var storedDoctor = await context.Doctors
            .FirstOrDefaultAsync(d => d.Id == result.Id);

        Assert.NotNull(storedDoctor);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeEmptyQualificationsToNull()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        var doctor = CreateDoctor();
        doctor.Qualifications = "   ";

        var result = await service.CreateAsync(doctor);

        Assert.Null(result.Qualifications);
    }

    [Fact]
    public async Task CreateAsync_ShouldForceDoctorToActive()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        var doctor = CreateDoctor(isActive: false);

        var result = await service.CreateAsync(doctor);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNullDoctor()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDoctor()
    {
        await using var context = CreateContext();

        var doctor = CreateDoctor();
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var service = new DoctorService(context);

        var result = await service.GetByIdAsync(doctor.Id);

        Assert.NotNull(result);
        Assert.Equal(doctor.Id, result.Id);
        Assert.Equal("Dr. John Smith", result.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        Assert.Null(await service.GetByIdAsync(0));
        Assert.Null(await service.GetByIdAsync(-1));
    }

    [Fact]
    public async Task GetByEmployeeNumberAsync_ShouldNormalizeEmployeeNumber()
    {
        await using var context = CreateContext();

        context.Doctors.Add(
            CreateDoctor(
                employeeNumber: "DOC-002",
                email: "jane@example.com"));

        await context.SaveChangesAsync();

        var service = new DoctorService(context);

        var result =
            await service.GetByEmployeeNumberAsync(" doc-002 ");

        Assert.NotNull(result);
        Assert.Equal("DOC-002", result.EmployeeNumber);
    }

    [Fact]
    public async Task GetByEmployeeNumberAsync_ShouldReturnNullForEmptyNumber()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        Assert.Null(
            await service.GetByEmployeeNumberAsync(""));

        Assert.Null(
            await service.GetByEmployeeNumberAsync("   "));
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldFindDoctorIgnoringCase()
    {
        await using var context = CreateContext();

        context.Doctors.Add(
            CreateDoctor(
                employeeNumber: "DOC-003",
                email: "sarah@example.com"));

        await context.SaveChangesAsync();

        var service = new DoctorService(context);

        var result =
            await service.GetByEmailAsync(" SARAH@EXAMPLE.COM ");

        Assert.NotNull(result);
        Assert.Equal("Dr. John Smith", result.FullName);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullForEmptyEmail()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        Assert.Null(await service.GetByEmailAsync(""));
        Assert.Null(await service.GetByEmailAsync("   "));
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullWhenDoctorDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        var result =
            await service.GetByEmailAsync("unknown@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDoctorsOrderedByFullName()
    {
        await using var context = CreateContext();

        context.Doctors.AddRange(
            CreateDoctor(
                fullName: "Dr. Zach Adams",
                employeeNumber: "DOC-004",
                email: "zach@example.com"),
            CreateDoctor(
                fullName: "Dr. Alice Adams",
                employeeNumber: "DOC-005",
                email: "alice@example.com"));

        await context.SaveChangesAsync();

        var service = new DoctorService(context);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Dr. Alice Adams", result[0].FullName);
        Assert.Equal("Dr. Zach Adams", result[1].FullName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndNormalizeDoctor()
    {
        await using var context = CreateContext();

        var doctor = CreateDoctor();
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var originalCreatedAt = doctor.CreatedAtUtc;

        doctor.FullName = "  Dr. Updated Smith  ";
        doctor.EmployeeNumber = " doc-999 ";
        doctor.Specialisation = " Cardiologist ";
        doctor.Email = " UPDATED@EXAMPLE.COM ";
        doctor.PhoneNumber = " 0839999999 ";
        doctor.Qualifications = " MBChB, FCP ";
        doctor.IsActive = false;

        var service = new DoctorService(context);

        await service.UpdateAsync(doctor);

        var updated = await context.Doctors
            .FirstAsync(d => d.Id == doctor.Id);

        Assert.Equal("Dr. Updated Smith", updated.FullName);
        Assert.Equal("DOC-999", updated.EmployeeNumber);
        Assert.Equal("Cardiologist", updated.Specialisation);
        Assert.Equal("updated@example.com", updated.Email);
        Assert.Equal("0839999999", updated.PhoneNumber);
        Assert.Equal("MBChB, FCP", updated.Qualifications);
        Assert.False(updated.IsActive);
        Assert.Equal(originalCreatedAt, updated.CreatedAtUtc);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectNullDoctor()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectInvalidId()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        var doctor = CreateDoctor();
        doctor.Id = 0;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(doctor));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenDoctorDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        var doctor = CreateDoctor();
        doctor.Id = 9999;

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(doctor));
    }

    [Fact]
    public async Task DeactivateAsync_ShouldDeactivateExistingDoctor()
    {
        await using var context = CreateContext();

        var doctor = CreateDoctor();

        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var service = new DoctorService(context);

        var result = await service.DeactivateAsync(doctor.Id);

        Assert.True(result);

        var deactivatedDoctor = await context.Doctors
            .FirstAsync(d => d.Id == doctor.Id);

        Assert.False(deactivatedDoctor.IsActive);
        Assert.NotNull(deactivatedDoctor.UpdatedAtUtc);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnTrueWhenAlreadyInactive()
    {
        await using var context = CreateContext();

        var doctor = CreateDoctor(isActive: false);

        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var service = new DoctorService(context);

        var result = await service.DeactivateAsync(doctor.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnFalseWhenDoctorDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        var result = await service.DeactivateAsync(9999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnFalseForInvalidId()
    {
        await using var context = CreateContext();
        var service = new DoctorService(context);

        Assert.False(await service.DeactivateAsync(0));
        Assert.False(await service.DeactivateAsync(-1));
    }
}