using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Services;

public class MedicalRecordServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Patient Patient, Doctor Doctor)>
        SeedDependenciesAsync(
            ApplicationDbContext context,
            bool patientActive = true,
            bool doctorActive = true)
    {
        var patient = new Patient
        {
            FullName = "Test Patient",
            PatientNumber = "PAT-400",
            PhoneNumber = "0824000000",
            Email = "patient400@example.com",
            IdNumber = "9001015009400",
            DateOfBirth = new DateTime(1990, 1, 1),
            IsActive = patientActive
        };

        var doctor = new Doctor
        {
            FullName = "Dr Test Doctor",
            EmployeeNumber = "DOC-400",
            Specialisation = "General Practice",
            Email = "doctor400@example.com",
            PhoneNumber = "0834000000",
            IsActive = doctorActive
        };

        context.Patients.Add(patient);
        context.Doctors.Add(doctor);

        await context.SaveChangesAsync();

        return (patient, doctor);
    }

    private static MedicalRecord CreateRecord(
        Patient patient,
        Doctor doctor,
        DateTime? recordedAtUtc = null)
    {
        return new MedicalRecord
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            RecordType = "Diagnosis",
            Description = "Patient diagnosed with migraine.",
            ClinicalNotes = "Follow-up required.",
            RecordedAtUtc =
                recordedAtUtc ?? DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateMedicalRecord()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        var result =
            await service.CreateAsync(record);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal(doctor.Id, result.DoctorId);
        Assert.Equal(
            "Diagnosis",
            result.RecordType);
        Assert.Equal(
            "Patient diagnosed with migraine.",
            result.Description);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeFields()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        record.RecordType =
            "  Diagnosis  ";

        record.Description =
            "  Patient diagnosed with migraine.  ";

        record.ClinicalNotes =
            "  Follow-up required.  ";

        var result =
            await service.CreateAsync(record);

        Assert.Equal(
            "Diagnosis",
            result.RecordType);

        Assert.Equal(
            "Patient diagnosed with migraine.",
            result.Description);

        Assert.Equal(
            "Follow-up required.",
            result.ClinicalNotes);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeBlankClinicalNotesToNull()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        record.ClinicalNotes = "   ";

        var result =
            await service.CreateAsync(record);

        Assert.Null(result.ClinicalNotes);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetDefaultRecordedDate()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        record.RecordedAtUtc = default;

        var before = DateTime.UtcNow;

        var result =
            await service.CreateAsync(record);

        var after = DateTime.UtcNow;

        Assert.InRange(
            result.RecordedAtUtc,
            before,
            after);

        Assert.Equal(
            DateTimeKind.Utc,
            result.RecordedAtUtc.Kind);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeUnspecifiedDateTimeToUtc()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        var date =
            new DateTime(
                2026,
                1,
                15,
                10,
                30,
                0,
                DateTimeKind.Unspecified);

        record.RecordedAtUtc = date;

        var result =
            await service.CreateAsync(record);

        Assert.Equal(
            DateTimeKind.Utc,
            result.RecordedAtUtc.Kind);

        Assert.Equal(
            date,
            result.RecordedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNullRecord()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidPatient()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (_, doctor) =
            await SeedDependenciesAsync(context);

        var record = new MedicalRecord
        {
            PatientId = 0,
            DoctorId = doctor.Id,
            RecordType = "Diagnosis",
            Description = "Test record"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(record));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidDoctor()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, _) =
            await SeedDependenciesAsync(context);

        var record = new MedicalRecord
        {
            PatientId = patient.Id,
            DoctorId = 0,
            RecordType = "Diagnosis",
            Description = "Test record"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(record));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingRecordType()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        record.RecordType = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(record));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingDescription()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        record.Description = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(record));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactivePatient()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(
                context,
                patientActive: false);

        var record =
            CreateRecord(patient, doctor);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(record));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactiveDoctor()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(
                context,
                doctorActive: false);

        var record =
            CreateRecord(patient, doctor);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(record));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMedicalRecordWithRelatedData()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        var result =
            await service.GetByIdAsync(record.Id);

        Assert.NotNull(result);
        Assert.Equal(record.Id, result.Id);
        Assert.NotNull(result.Patient);
        Assert.NotNull(result.Doctor);
        Assert.Equal(patient.Id, result.Patient.Id);
        Assert.Equal(doctor.Id, result.Doctor.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        Assert.Null(
            await service.GetByIdAsync(0));

        Assert.Null(
            await service.GetByIdAsync(-1));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenRecordDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var result =
            await service.GetByIdAsync(9999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPatientAsync_ShouldReturnPatientRecordsInDescendingDateOrder()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var older =
            CreateRecord(
                patient,
                doctor,
                DateTime.UtcNow.AddDays(-2));

        older.Description = "Older record";

        var newer =
            CreateRecord(
                patient,
                doctor,
                DateTime.UtcNow.AddDays(-1));

        newer.Description = "Newer record";

        context.MedicalRecords.AddRange(
            older,
            newer);

        await context.SaveChangesAsync();

        var result =
            await service.GetByPatientAsync(
                patient.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(
            "Newer record",
            result[0].Description);
        Assert.Equal(
            "Older record",
            result[1].Description);
    }

    [Fact]
    public async Task GetByPatientAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        Assert.Empty(
            await service.GetByPatientAsync(0));

        Assert.Empty(
            await service.GetByPatientAsync(-1));
    }

    [Fact]
    public async Task GetByDoctorAsync_ShouldReturnDoctorRecordsInDescendingDateOrder()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var older =
            CreateRecord(
                patient,
                doctor,
                DateTime.UtcNow.AddDays(-2));

        older.Description = "Older record";

        var newer =
            CreateRecord(
                patient,
                doctor,
                DateTime.UtcNow.AddDays(-1));

        newer.Description = "Newer record";

        context.MedicalRecords.AddRange(
            older,
            newer);

        await context.SaveChangesAsync();

        var result =
            await service.GetByDoctorAsync(
                doctor.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(
            "Newer record",
            result[0].Description);
        Assert.Equal(
            "Older record",
            result[1].Description);
    }

    [Fact]
    public async Task GetByDoctorAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        Assert.Empty(
            await service.GetByDoctorAsync(0));

        Assert.Empty(
            await service.GetByDoctorAsync(-1));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndNormalizeRecord()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        record.RecordType =
            "  Follow-up  ";

        record.Description =
            "  Patient condition improved.  ";

        record.ClinicalNotes =
            "  Continue monitoring.  ";

        await service.UpdateAsync(record);

        var updated =
            await context.MedicalRecords
                .FirstAsync(
                    m => m.Id == record.Id);

        Assert.Equal(
            "Follow-up",
            updated.RecordType);

        Assert.Equal(
            "Patient condition improved.",
            updated.Description);

        Assert.Equal(
            "Continue monitoring.",
            updated.ClinicalNotes);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNormalizeBlankClinicalNotesToNull()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        record.ClinicalNotes = "   ";

        await service.UpdateAsync(record);

        var updated =
            await context.MedicalRecords
                .FirstAsync(
                    m => m.Id == record.Id);

        Assert.Null(updated.ClinicalNotes);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectNullRecord()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectInvalidId()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var record = new MedicalRecord
        {
            Id = 0,
            PatientId = 1,
            DoctorId = 1,
            RecordType = "Diagnosis",
            Description = "Test record"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(record));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenRecordDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var record = new MedicalRecord
        {
            Id = 9999,
            PatientId = 1,
            DoctorId = 1,
            RecordType = "Diagnosis",
            Description = "Test record"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(record));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMissingRecordType()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        record.RecordType = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(record));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMissingDescription()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        record.Description = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(record));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventPatientRelationshipChanges()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        record.PatientId = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(record));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventDoctorRelationshipChanges()
    {
        await using var context = CreateContext();

        var service = new MedicalRecordService(context);

        var (patient, doctor) =
            await SeedDependenciesAsync(context);

        var record =
            CreateRecord(patient, doctor);

        context.MedicalRecords.Add(record);
        await context.SaveChangesAsync();

        record.DoctorId = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(record));
    }
}