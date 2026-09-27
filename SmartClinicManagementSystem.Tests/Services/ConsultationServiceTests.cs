using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Services;

public class ConsultationServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Patient Patient, Doctor Doctor, Appointment Appointment)>
        SeedConsultationDependenciesAsync(
            ApplicationDbContext context,
            string appointmentStatus = "Scheduled")
    {
        var patient = new Patient
        {
            FullName = "Test Patient",
            PatientNumber = "PAT-200",
            PhoneNumber = "0820000000",
            Email = "patient200@example.com",
            IdNumber = "9001015009200",
            DateOfBirth = new DateTime(1990, 1, 1),
            IsActive = true
        };

        var doctor = new Doctor
        {
            FullName = "Dr Test Doctor",
            EmployeeNumber = "DOC-200",
            Specialisation = "General Practice",
            Email = "doctor200@example.com",
            PhoneNumber = "0830000000",
            IsActive = true
        };

        context.Patients.Add(patient);
        context.Doctors.Add(doctor);

        await context.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime = DateTime.UtcNow.AddDays(1),
            Status = appointmentStatus,
            Reason = "General consultation"
        };

        context.Appointments.Add(appointment);

        await context.SaveChangesAsync();

        return (patient, doctor, appointment);
    }

    private static Consultation CreateConsultation(
        Patient patient,
        Doctor doctor,
        Appointment appointment)
    {
        return new Consultation
        {
            AppointmentId = appointment.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Symptoms = "Headache and fatigue",
            Diagnosis = "Tension headache",
            TreatmentPlan = "Rest and hydration",
            ClinicalNotes = "Patient advised to monitor symptoms.",
            ConsultationDateUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateConsultationAndCompleteAppointment()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        var result = await service.CreateAsync(consultation);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal(doctor.Id, result.DoctorId);
        Assert.Equal(appointment.Id, result.AppointmentId);
        Assert.Equal("Tension headache", result.Diagnosis);

        var updatedAppointment = await context.Appointments
            .FirstAsync(a => a.Id == appointment.Id);

        Assert.Equal("Completed", updatedAppointment.Status);
        Assert.NotNull(updatedAppointment.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeClinicalFields()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        consultation.Symptoms = "  Headache  ";
        consultation.Diagnosis = "  Tension headache  ";
        consultation.TreatmentPlan = "  Rest and hydration  ";
        consultation.ClinicalNotes = "  Follow-up required.  ";

        var result = await service.CreateAsync(consultation);

        Assert.Equal("Headache", result.Symptoms);
        Assert.Equal("Tension headache", result.Diagnosis);
        Assert.Equal(
            "Rest and hydration",
            result.TreatmentPlan);
        Assert.Equal(
            "Follow-up required.",
            result.ClinicalNotes);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeBlankOptionalFieldsToNull()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        consultation.TreatmentPlan = "   ";
        consultation.ClinicalNotes = "   ";

        var result = await service.CreateAsync(consultation);

        Assert.Null(result.TreatmentPlan);
        Assert.Null(result.ClinicalNotes);
    }

    [Fact]
    public async Task CreateAsync_ShouldUseCurrentUtcTimeWhenDateIsDefault()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        consultation.ConsultationDateUtc = default;

        var before = DateTime.UtcNow;

        var result = await service.CreateAsync(consultation);

        var after = DateTime.UtcNow;

        Assert.InRange(
            result.ConsultationDateUtc,
            before,
            after);

        Assert.Equal(
            DateTimeKind.Utc,
            result.ConsultationDateUtc.Kind);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeUnspecifiedDateToUtc()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        consultation.ConsultationDateUtc =
            new DateTime(2026, 1, 15, 10, 30, 0);

        var result = await service.CreateAsync(consultation);

        Assert.Equal(
            DateTimeKind.Utc,
            result.ConsultationDateUtc.Kind);

        Assert.Equal(
            new DateTime(2026, 1, 15, 10, 30, 0),
            result.ConsultationDateUtc);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNullConsultation()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidAppointmentId()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var consultation = new Consultation
        {
            AppointmentId = 0,
            PatientId = 1,
            DoctorId = 1,
            Symptoms = "Headache",
            Diagnosis = "Tension headache"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidPatientId()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var consultation = new Consultation
        {
            AppointmentId = 1,
            PatientId = 0,
            DoctorId = 1,
            Symptoms = "Headache",
            Diagnosis = "Tension headache"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidDoctorId()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var consultation = new Consultation
        {
            AppointmentId = 1,
            PatientId = 1,
            DoctorId = 0,
            Symptoms = "Headache",
            Diagnosis = "Tension headache"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingSymptoms()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        consultation.Symptoms = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingDiagnosis()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        consultation.Diagnosis = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingAppointment()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, _) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = new Consultation
        {
            AppointmentId = 9999,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Symptoms = "Headache",
            Diagnosis = "Tension headache"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectPatientAppointmentMismatch()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var secondPatient = new Patient
        {
            FullName = "Second Patient",
            PatientNumber = "PAT-201",
            PhoneNumber = "0821111111",
            Email = "patient201@example.com",
            IdNumber = "9001015009201",
            DateOfBirth = new DateTime(1991, 1, 1),
            IsActive = true
        };

        context.Patients.Add(secondPatient);
        await context.SaveChangesAsync();

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        consultation.PatientId = secondPatient.Id;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDoctorAppointmentMismatch()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var secondDoctor = new Doctor
        {
            FullName = "Dr Second Doctor",
            EmployeeNumber = "DOC-201",
            Specialisation = "Cardiology",
            Email = "doctor201@example.com",
            PhoneNumber = "0831111111",
            IsActive = true
        };

        context.Doctors.Add(secondDoctor);
        await context.SaveChangesAsync();

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        consultation.DoctorId = secondDoctor.Id;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectCancelledAppointment()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(
                context,
                "Cancelled");

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectCompletedAppointment()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(
                context,
                "Completed");

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNoShowAppointment()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(
                context,
                "No-Show");

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactivePatient()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        patient.IsActive = false;
        await context.SaveChangesAsync();

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactiveDoctor()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        doctor.IsActive = false;
        await context.SaveChangesAsync();

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(consultation));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateConsultationForAppointment()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var firstConsultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        await service.CreateAsync(firstConsultation);

        var secondConsultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(secondConsultation));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnConsultationWithRelatedData()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        var result = await service.GetByIdAsync(consultation.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.Patient);
        Assert.NotNull(result.Doctor);
        Assert.NotNull(result.Appointment);
        Assert.NotNull(result.Prescriptions);
        Assert.Equal(patient.Id, result.Patient.Id);
        Assert.Equal(doctor.Id, result.Doctor.Id);
        Assert.Equal(appointment.Id, result.Appointment.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        Assert.Null(await service.GetByIdAsync(0));
        Assert.Null(await service.GetByIdAsync(-1));
    }

    [Fact]
    public async Task GetByAppointmentIdAsync_ShouldReturnConsultation()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        var result =
            await service.GetByAppointmentIdAsync(
                appointment.Id);

        Assert.NotNull(result);
        Assert.Equal(
            consultation.Id,
            result.Id);
        Assert.NotNull(result.Appointment);
        Assert.Equal(
            appointment.Id,
            result.Appointment.Id);
    }

    [Fact]
    public async Task GetByAppointmentIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        Assert.Null(
            await service.GetByAppointmentIdAsync(0));

        Assert.Null(
            await service.GetByAppointmentIdAsync(-1));
    }

    [Fact]
    public async Task GetByPatientAsync_ShouldReturnPatientConsultationsInDescendingDateOrder()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var older = CreateConsultation(
            patient,
            doctor,
            appointment);

        older.ConsultationDateUtc =
            DateTime.UtcNow.AddDays(-2);

        context.Consultations.Add(older);

        var newerAppointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime = DateTime.UtcNow.AddDays(2),
            Status = "Scheduled",
            Reason = "Follow-up"
        };

        context.Appointments.Add(newerAppointment);
        await context.SaveChangesAsync();

        var newer = CreateConsultation(
            patient,
            doctor,
            newerAppointment);

        newer.ConsultationDateUtc =
            DateTime.UtcNow.AddDays(-1);

        context.Consultations.Add(newer);
        await context.SaveChangesAsync();

        var result =
            await service.GetByPatientAsync(patient.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public async Task GetByPatientAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var result =
            await service.GetByPatientAsync(0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByDoctorAsync_ShouldReturnDoctorConsultations()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        var result =
            await service.GetByDoctorAsync(doctor.Id);

        Assert.Single(result);
        Assert.Equal(
            doctor.Id,
            result[0].DoctorId);
        Assert.NotNull(result[0].Patient);
        Assert.NotNull(result[0].Doctor);
    }

    [Fact]
    public async Task GetByDoctorAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var result =
            await service.GetByDoctorAsync(0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndNormalizeConsultation()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        var originalDate =
            consultation.ConsultationDateUtc;

        consultation.Symptoms =
            "  Severe headache  ";

        consultation.Diagnosis =
            "  Migraine  ";

        consultation.TreatmentPlan =
            "  Medication and rest  ";

        consultation.ClinicalNotes =
            "  Follow-up in two weeks.  ";

        await service.UpdateAsync(consultation);

        var updated = await context.Consultations
            .FirstAsync(
                c => c.Id == consultation.Id);

        Assert.Equal(
            "Severe headache",
            updated.Symptoms);

        Assert.Equal(
            "Migraine",
            updated.Diagnosis);

        Assert.Equal(
            "Medication and rest",
            updated.TreatmentPlan);

        Assert.Equal(
            "Follow-up in two weeks.",
            updated.ClinicalNotes);

        Assert.Equal(
            originalDate,
            updated.ConsultationDateUtc);

        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNormalizeBlankOptionalFieldsToNull()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        consultation.TreatmentPlan = "   ";
        consultation.ClinicalNotes = "   ";

        await service.UpdateAsync(consultation);

        var updated = await context.Consultations
            .FirstAsync(
                c => c.Id == consultation.Id);

        Assert.Null(updated.TreatmentPlan);
        Assert.Null(updated.ClinicalNotes);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectNullConsultation()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectInvalidId()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var consultation = new Consultation
        {
            Id = 0,
            PatientId = 1,
            DoctorId = 1,
            AppointmentId = 1,
            Symptoms = "Headache",
            Diagnosis = "Migraine"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(consultation));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenConsultationDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var consultation = new Consultation
        {
            Id = 9999,
            PatientId = 1,
            DoctorId = 1,
            AppointmentId = 1,
            Symptoms = "Headache",
            Diagnosis = "Migraine"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(consultation));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMissingSymptoms()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        consultation.Symptoms = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(consultation));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMissingDiagnosis()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        consultation.Diagnosis = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(consultation));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventRelationshipChanges()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        consultation.PatientId = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(consultation));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventDoctorRelationshipChanges()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        consultation.DoctorId = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(consultation));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventAppointmentRelationshipChanges()
    {
        await using var context = CreateContext();

        var service = new ConsultationService(context);

        var (patient, doctor, appointment) =
            await SeedConsultationDependenciesAsync(context);

        var consultation = CreateConsultation(
            patient,
            doctor,
            appointment);

        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();

        consultation.AppointmentId = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(consultation));
    }
}