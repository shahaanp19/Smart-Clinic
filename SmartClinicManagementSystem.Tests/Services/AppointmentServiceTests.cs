using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Services;

public class AppointmentServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Patient Patient, Doctor Doctor)>
        SeedPatientAndDoctorAsync(
            ApplicationDbContext context)
    {
        var patient = new Patient
        {
            FullName = "Test Patient",
            PatientNumber = $"PAT-{Guid.NewGuid():N}",
            PhoneNumber = "0110000000",
            Email = $"patient-{Guid.NewGuid():N}@test.com",
            IdNumber = Guid.NewGuid().ToString("N"),
            DateOfBirth = new DateTime(1995, 1, 1),
            Gender = "Other",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctor = new Doctor
        {
            FullName = "Test Doctor",
            EmployeeNumber = $"DOC-{Guid.NewGuid():N}",
            Specialisation = "General Practice",
            Email = $"doctor-{Guid.NewGuid():N}@test.com",
            PhoneNumber = "0110000001",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await context.Patients.AddAsync(patient);
        await context.Doctors.AddAsync(doctor);
        await context.SaveChangesAsync();

        return (patient, doctor);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateValidAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1),
            Status = "scheduled",
            Reason = "  Routine consultation  ",
            Notes = "  Follow-up required  "
        };

        var result =
            await service.CreateAsync(appointment);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Scheduled", result.Status);
        Assert.Equal(
            "Routine consultation",
            result.Reason);
        Assert.Equal(
            "Follow-up required",
            result.Notes);
        Assert.NotEqual(default, result.CreatedAtUtc);
        Assert.Null(result.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_ShouldDefaultMissingStatusToScheduled()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1)
        };

        var result =
            await service.CreateAsync(appointment);

        Assert.Equal("Scheduled", result.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeOptionalTextToNull()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1),
            Reason = "   ",
            Notes = "   "
        };

        var result =
            await service.CreateAsync(appointment);

        Assert.Null(result.Reason);
        Assert.Null(result.Notes);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNullAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidPatientId()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var appointment = new Appointment
        {
            PatientId = 0,
            DoctorId = 1,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1)
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(appointment));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidDoctorId()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var appointment = new Appointment
        {
            PatientId = 1,
            DoctorId = 0,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1)
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(appointment));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectPastAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddMinutes(-5)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(appointment));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactivePatient()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        patient.IsActive = false;
        await context.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(appointment));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactiveDoctor()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        doctor.IsActive = false;
        await context.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(appointment));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidStatus()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1),
            Status = "InvalidStatus"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(appointment));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectOccupiedSlot()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointmentTime =
            DateTime.UtcNow.AddDays(1);

        await service.CreateAsync(
            new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDateTime = appointmentTime,
                Status = "Scheduled"
            });

        var secondPatient = new Patient
        {
            FullName = "Second Patient",
            PatientNumber = $"PAT-{Guid.NewGuid():N}",
            PhoneNumber = "0110000002",
            Email = $"second-{Guid.NewGuid():N}@test.com",
            IdNumber = Guid.NewGuid().ToString("N"),
            DateOfBirth = new DateTime(1996, 1, 1),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await context.Patients.AddAsync(secondPatient);
        await context.SaveChangesAsync();

        var duplicate = new Appointment
        {
            PatientId = secondPatient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime = appointmentTime
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(duplicate));
    }

    [Fact]
    public async Task IsSlotAvailableAsync_ShouldReturnFalseForActiveBooking()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointmentTime =
            DateTime.UtcNow.AddDays(1);

        await service.CreateAsync(
            new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDateTime = appointmentTime,
                Status = "Scheduled"
            });

        var available =
            await service.IsSlotAvailableAsync(
                doctor.Id,
                appointmentTime);

        Assert.False(available);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_ShouldReturnTrueForCancelledBooking()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointmentTime =
            DateTime.UtcNow.AddDays(1);

        var appointment =
            await service.CreateAsync(
                new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime = appointmentTime
                });

        await service.CancelAsync(appointment.Id);

        var available =
            await service.IsSlotAvailableAsync(
                doctor.Id,
                appointmentTime);

        Assert.True(available);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_ShouldAllowExcludedAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointmentTime =
            DateTime.UtcNow.AddDays(1);

        var appointment =
            await service.CreateAsync(
                new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime = appointmentTime
                });

        var available =
            await service.IsSlotAvailableAsync(
                doctor.Id,
                appointmentTime,
                appointment.Id);

        Assert.True(available);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_ShouldReturnFalseForInvalidDoctorId()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var available =
            await service.IsSlotAvailableAsync(
                0,
                DateTime.UtcNow.AddDays(1));

        Assert.False(available);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAppointmentWithRelatedEntities()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment =
            await service.CreateAsync(
                new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime =
                        DateTime.UtcNow.AddDays(1)
                });

        var result =
            await service.GetByIdAsync(appointment.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.Patient);
        Assert.NotNull(result.Doctor);
        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal(doctor.Id, result.DoctorId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        Assert.Null(await service.GetByIdAsync(0));
        Assert.Null(await service.GetByIdAsync(-1));
    }

    [Fact]
    public async Task GetByDoctorAsync_ShouldReturnAppointmentsInDateRange()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var first =
            DateTime.UtcNow.AddDays(1);

        var second =
            DateTime.UtcNow.AddDays(2);

        await service.CreateAsync(
            new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDateTime = first
            });

        await service.CreateAsync(
            new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDateTime = second
            });

        var result =
            await service.GetByDoctorAsync(
                doctor.Id,
                first.AddHours(-1),
                second.AddHours(1));

        Assert.Equal(2, result.Count);
        Assert.True(
            result[0].AppointmentDateTime <=
            result[1].AppointmentDateTime);
    }

    [Fact]
    public async Task GetByDoctorAsync_ShouldRejectInvalidDateRange()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetByDoctorAsync(
                1,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public async Task GetByPatientAsync_ShouldReturnAppointmentsInDateRange()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointmentTime =
            DateTime.UtcNow.AddDays(1);

        await service.CreateAsync(
            new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDateTime = appointmentTime
            });

        var result =
            await service.GetByPatientAsync(
                patient.Id,
                appointmentTime.AddHours(-1),
                appointmentTime.AddHours(1));

        Assert.Single(result);
        Assert.Equal(
            patient.Id,
            result[0].PatientId);
    }

    [Fact]
    public async Task GetByPatientAsync_ShouldRejectInvalidDateRange()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetByPatientAsync(
                1,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var originalTime =
            DateTime.UtcNow.AddDays(1);

        var updatedTime =
            DateTime.UtcNow.AddDays(2);

        var appointment =
            await service.CreateAsync(
                new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime = originalTime,
                    Reason = "Initial consultation"
                });

        appointment.AppointmentDateTime = updatedTime;
        appointment.Status = "confirmed";
        appointment.Reason = "  Follow-up consultation  ";
        appointment.Notes = "  Updated notes  ";

        await service.UpdateAsync(appointment);

        var updated = await context.Appointments
            .FirstAsync(a => a.Id == appointment.Id);

        Assert.Equal(updatedTime, updated.AppointmentDateTime);
        Assert.Equal("Confirmed", updated.Status);
        Assert.Equal(
            "Follow-up consultation",
            updated.Reason);
        Assert.Equal(
            "Updated notes",
            updated.Notes);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectNullAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectInvalidId()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var appointment = new Appointment
        {
            Id = 0,
            PatientId = 1,
            DoctorId = 1,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1)
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(appointment));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenAppointmentDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var appointment = new Appointment
        {
            Id = 9999,
            PatientId = 1,
            DoctorId = 1,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(1)
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(appointment));
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelExistingAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment =
            await service.CreateAsync(
                new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime =
                        DateTime.UtcNow.AddDays(1)
                });

        var result =
            await service.CancelAsync(appointment.Id);

        Assert.True(result);

        var stored = await context.Appointments
            .FirstAsync(a => a.Id == appointment.Id);

        Assert.Equal("Cancelled", stored.Status);
        Assert.NotNull(stored.UpdatedAtUtc);
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnTrueWhenAlreadyCancelled()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment =
            await service.CreateAsync(
                new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime =
                        DateTime.UtcNow.AddDays(1)
                });

        await service.CancelAsync(appointment.Id);

        var result =
            await service.CancelAsync(appointment.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task CancelAsync_ShouldRejectCompletedAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var (patient, doctor) =
            await SeedPatientAndDoctorAsync(context);

        var appointment =
            await service.CreateAsync(
                new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    AppointmentDateTime =
                        DateTime.UtcNow.AddDays(1)
                });

        appointment.Status = "Completed";
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelAsync(appointment.Id));
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnFalseForUnknownAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        var result =
            await service.CancelAsync(9999);

        Assert.False(result);
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnFalseForInvalidId()
    {
        await using var context = CreateContext();
        var service = new AppointmentService(context);

        Assert.False(await service.CancelAsync(0));
        Assert.False(await service.CancelAsync(-1));
    }
}