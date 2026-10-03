using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Services;

public class PatientServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Patient CreatePatient(
        string fullName = "John Doe",
        string patientNumber = "PAT-001",
        string email = "john@example.com",
        bool isActive = true)
    {
        return new Patient
        {
            FullName = fullName,
            PatientNumber = patientNumber,
            PhoneNumber = "0821234567",
            Email = email,
            IdNumber = "9001015009087",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "Male",
            Address = "123 Main Street",
            IsActive = isActive
        };
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndNormalizePatient()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        var patient = new Patient
        {
            FullName = "  John Doe  ",
            PatientNumber = " pat-001 ",
            PhoneNumber = " 0821234567 ",
            Email = " JOHN@EXAMPLE.COM ",
            IdNumber = " 9001015009087 ",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = " Male ",
            Address = " 123 Main Street "
        };

        var before = DateTime.UtcNow;

        var result = await service.CreateAsync(patient);

        var after = DateTime.UtcNow;

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("PAT-001", result.PatientNumber);
        Assert.Equal("0821234567", result.PhoneNumber);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("9001015009087", result.IdNumber);
        Assert.Equal("Male", result.Gender);
        Assert.Equal("123 Main Street", result.Address);
        Assert.True(result.IsActive);
        Assert.InRange(result.CreatedAtUtc, before, after);
        Assert.Null(result.UpdatedAtUtc);

        var storedPatient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == result.Id);

        Assert.NotNull(storedPatient);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeOptionalFieldsToNull()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        var patient = CreatePatient();

        patient.Email = "   ";
        patient.Gender = "   ";
        patient.Address = "   ";

        var result = await service.CreateAsync(patient);

        Assert.Null(result.Email);
        Assert.Null(result.Gender);
        Assert.Null(result.Address);
    }

    [Fact]
    public async Task CreateAsync_ShouldForcePatientToActive()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        var patient = CreatePatient(isActive: false);

        var result = await service.CreateAsync(patient);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNullPatient()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPatient()
    {
        await using var context = CreateContext();

        var patient = CreatePatient(
            fullName: "Jane Smith",
            patientNumber: "PAT-002",
            email: "jane@example.com");

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var service = new PatientService(context);

        var result = await service.GetByIdAsync(patient.Id);

        Assert.NotNull(result);
        Assert.Equal(patient.Id, result.Id);
        Assert.Equal("Jane Smith", result.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        Assert.Null(await service.GetByIdAsync(0));
        Assert.Null(await service.GetByIdAsync(-1));
    }

    [Fact]
    public async Task GetByPatientNumberAsync_ShouldNormalizePatientNumber()
    {
        await using var context = CreateContext();

        context.Patients.Add(
            CreatePatient(
                fullName: "Michael Brown",
                patientNumber: "PAT-003",
                email: "michael@example.com"));

        await context.SaveChangesAsync();

        var service = new PatientService(context);

        var result =
            await service.GetByPatientNumberAsync("  pat-003 ");

        Assert.NotNull(result);
        Assert.Equal("PAT-003", result.PatientNumber);
    }

    [Fact]
    public async Task GetByPatientNumberAsync_ShouldReturnNullForEmptyNumber()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        Assert.Null(
            await service.GetByPatientNumberAsync(""));

        Assert.Null(
            await service.GetByPatientNumberAsync("   "));
    }

    [Fact]
    public async Task GetByIdNumberAsync_ShouldFindPatient()
    {
        await using var context = CreateContext();

        context.Patients.Add(
            CreatePatient());

        await context.SaveChangesAsync();

        var service = new PatientService(context);

        var result =
            await service.GetByIdNumberAsync(" 9001015009087 ");

        Assert.NotNull(result);
        Assert.Equal("9001015009087", result.IdNumber);
    }

    [Fact]
    public async Task GetByIdNumberAsync_ShouldReturnNullForEmptyIdNumber()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        Assert.Null(
            await service.GetByIdNumberAsync(""));

        Assert.Null(
            await service.GetByIdNumberAsync("   "));
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldFindPatientIgnoringCase()
    {
        await using var context = CreateContext();

        context.Patients.Add(
            CreatePatient(
                fullName: "Sarah Williams",
                patientNumber: "PAT-004",
                email: "sarah@example.com"));

        await context.SaveChangesAsync();

        var service = new PatientService(context);

        var result =
            await service.GetByEmailAsync(" SARAH@EXAMPLE.COM ");

        Assert.NotNull(result);
        Assert.Equal("Sarah Williams", result.FullName);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullForEmptyEmail()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        Assert.Null(await service.GetByEmailAsync(""));
        Assert.Null(await service.GetByEmailAsync("   "));
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullWhenPatientHasNoEmail()
    {
        await using var context = CreateContext();

        var patient = CreatePatient();
        patient.Email = null;

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var service = new PatientService(context);

        var result =
            await service.GetByEmailAsync("john@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenPatientDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        var result = await service.GetByIdAsync(9999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPatientsOrderedByFullName()
    {
        await using var context = CreateContext();

        context.Patients.AddRange(
            CreatePatient(
                fullName: "Zach Adams",
                patientNumber: "PAT-006",
                email: "zach@example.com"),
            CreatePatient(
                fullName: "Alice Adams",
                patientNumber: "PAT-007",
                email: "alice@example.com"));

        await context.SaveChangesAsync();

        var service = new PatientService(context);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice Adams", result[0].FullName);
        Assert.Equal("Zach Adams", result[1].FullName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePatientAndNormalizeValues()
    {
        await using var context = CreateContext();

        var patient = CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var originalCreatedAt = patient.CreatedAtUtc;

        patient.FullName = "  Updated Patient  ";
        patient.PatientNumber = " pat-999 ";
        patient.PhoneNumber = " 0839999999 ";
        patient.Email = " UPDATED@EXAMPLE.COM ";
        patient.IdNumber = " 9101015009088 ";
        patient.Gender = " Female ";
        patient.Address = " 456 Updated Street ";
        patient.IsActive = false;

        var service = new PatientService(context);

        await service.UpdateAsync(patient);

        var updated = await context.Patients
            .FirstAsync(p => p.Id == patient.Id);

        Assert.Equal("Updated Patient", updated.FullName);
        Assert.Equal("PAT-999", updated.PatientNumber);
        Assert.Equal("0839999999", updated.PhoneNumber);
        Assert.Equal("updated@example.com", updated.Email);
        Assert.Equal("9101015009088", updated.IdNumber);
        Assert.Equal("Female", updated.Gender);
        Assert.Equal("456 Updated Street", updated.Address);
        Assert.False(updated.IsActive);
        Assert.Equal(originalCreatedAt, updated.CreatedAtUtc);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectNullPatient()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectInvalidId()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        var patient = CreatePatient();
        patient.Id = 0;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(patient));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenPatientDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        var patient = CreatePatient();
        patient.Id = 9999;

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(patient));
    }

    [Fact]
    public async Task DeactivateAsync_ShouldDeactivateExistingPatient()
    {
        await using var context = CreateContext();

        var patient = CreatePatient(
            fullName: "David Jones",
            patientNumber: "PAT-005",
            email: "david@example.com");

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var service = new PatientService(context);

        var result = await service.DeactivateAsync(patient.Id);

        Assert.True(result);

        var deactivatedPatient = await context.Patients
            .FirstAsync(p => p.Id == patient.Id);

        Assert.False(deactivatedPatient.IsActive);
        Assert.NotNull(deactivatedPatient.UpdatedAtUtc);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnTrueWhenAlreadyInactive()
    {
        await using var context = CreateContext();

        var patient = CreatePatient(isActive: false);

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var service = new PatientService(context);

        var result = await service.DeactivateAsync(patient.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnFalseWhenPatientDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        var result = await service.DeactivateAsync(9999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnFalseForInvalidId()
    {
        await using var context = CreateContext();
        var service = new PatientService(context);

        Assert.False(await service.DeactivateAsync(0));
        Assert.False(await service.DeactivateAsync(-1));
    }
}