using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Services;

public class PrescriptionServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(
        Patient Patient,
        Doctor Doctor,
        Consultation Consultation)>
        SeedDependenciesAsync(
            ApplicationDbContext context,
            bool patientActive = true,
            bool doctorActive = true)
    {
        var patient = new Patient
        {
            FullName = "Test Patient",
            PatientNumber = "PAT-300",
            PhoneNumber = "0820000000",
            Email = "patient300@example.com",
            IdNumber = "9001015009300",
            DateOfBirth = new DateTime(1990, 1, 1),
            IsActive = patientActive
        };

        var doctor = new Doctor
        {
            FullName = "Dr Test Doctor",
            EmployeeNumber = "DOC-300",
            Specialisation = "General Practice",
            Email = "doctor300@example.com",
            PhoneNumber = "0830000000",
            IsActive = doctorActive
        };

        context.Patients.Add(patient);
        context.Doctors.Add(doctor);

        await context.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime = DateTime.UtcNow.AddDays(1),
            Status = "Scheduled",
            Reason = "Follow-up consultation"
        };

        context.Appointments.Add(appointment);

        await context.SaveChangesAsync();

        var consultation = new Consultation
        {
            AppointmentId = appointment.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Symptoms = "Persistent headache",
            Diagnosis = "Migraine",
            TreatmentPlan = "Medication and rest",
            ClinicalNotes = "Follow-up required.",
            ConsultationDateUtc = DateTime.UtcNow
        };

        context.Consultations.Add(consultation);

        await context.SaveChangesAsync();

        return (patient, doctor, consultation);
    }

    private static Prescription CreatePrescription(
        Patient patient,
        Doctor doctor,
        Consultation consultation)
    {
        return new Prescription
        {
            ConsultationId = consultation.Id,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            MedicationName = "Paracetamol",
            Dosage = "500 mg",
            Frequency = "Every 6 hours",
            Duration = "5 days",
            Instructions = "Take after meals.",
            PrescribedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateAsync_ShouldCreatePrescription()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        var result =
            await service.CreateAsync(prescription);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Paracetamol", result.MedicationName);
        Assert.Equal("500 mg", result.Dosage);
        Assert.Equal(consultation.Id, result.ConsultationId);
        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal(doctor.Id, result.DoctorId);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeFields()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.MedicationName = "  Paracetamol  ";
        prescription.Dosage = "  500 mg  ";
        prescription.Frequency = "  Every 6 hours  ";
        prescription.Duration = "  5 days  ";
        prescription.Instructions = "  Take after meals.  ";

        var result =
            await service.CreateAsync(prescription);

        Assert.Equal("Paracetamol", result.MedicationName);
        Assert.Equal("500 mg", result.Dosage);
        Assert.Equal("Every 6 hours", result.Frequency);
        Assert.Equal("5 days", result.Duration);
        Assert.Equal("Take after meals.", result.Instructions);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeBlankInstructionsToNull()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.Instructions = "   ";

        var result =
            await service.CreateAsync(prescription);

        Assert.Null(result.Instructions);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetDefaultPrescriptionDate()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.PrescribedAtUtc = default;

        var before = DateTime.UtcNow;

        var result =
            await service.CreateAsync(prescription);

        var after = DateTime.UtcNow;

        Assert.InRange(
            result.PrescribedAtUtc,
            before,
            after);

        Assert.Equal(
            DateTimeKind.Utc,
            result.PrescribedAtUtc.Kind);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeUnspecifiedDateTimeToUtc()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        var date =
            new DateTime(
                2026,
                1,
                15,
                10,
                30,
                0,
                DateTimeKind.Unspecified);

        prescription.PrescribedAtUtc = date;

        var result =
            await service.CreateAsync(prescription);

        Assert.Equal(
            DateTimeKind.Utc,
            result.PrescribedAtUtc.Kind);

        Assert.Equal(
            date,
            result.PrescribedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectNullPrescription()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidConsultationId()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var prescription = new Prescription
        {
            ConsultationId = 0,
            PatientId = 1,
            DoctorId = 1,
            MedicationName = "Paracetamol",
            Dosage = "500 mg",
            Frequency = "Every 6 hours",
            Duration = "5 days"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidPatientId()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var prescription = new Prescription
        {
            ConsultationId = 1,
            PatientId = 0,
            DoctorId = 1,
            MedicationName = "Paracetamol",
            Dosage = "500 mg",
            Frequency = "Every 6 hours",
            Duration = "5 days"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidDoctorId()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var prescription = new Prescription
        {
            ConsultationId = 1,
            PatientId = 1,
            DoctorId = 0,
            MedicationName = "Paracetamol",
            Dosage = "500 mg",
            Frequency = "Every 6 hours",
            Duration = "5 days"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingMedicationName()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.MedicationName = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingDosage()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.Dosage = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingFrequency()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.Frequency = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingDuration()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.Duration = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingConsultation()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, _) =
            await SeedDependenciesAsync(context);

        var prescription = new Prescription
        {
            ConsultationId = 9999,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            MedicationName = "Paracetamol",
            Dosage = "500 mg",
            Frequency = "Every 6 hours",
            Duration = "5 days"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectPatientConsultationMismatch()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var secondPatient = new Patient
        {
            FullName = "Second Patient",
            PatientNumber = "PAT-301",
            PhoneNumber = "0821111111",
            Email = "patient301@example.com",
            IdNumber = "9001015009301",
            DateOfBirth = new DateTime(1991, 1, 1),
            IsActive = true
        };

        context.Patients.Add(secondPatient);
        await context.SaveChangesAsync();

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.PatientId = secondPatient.Id;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDoctorConsultationMismatch()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var secondDoctor = new Doctor
        {
            FullName = "Dr Second Doctor",
            EmployeeNumber = "DOC-301",
            Specialisation = "Cardiology",
            Email = "doctor301@example.com",
            PhoneNumber = "0831111111",
            IsActive = true
        };

        context.Doctors.Add(secondDoctor);
        await context.SaveChangesAsync();

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        prescription.DoctorId = secondDoctor.Id;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactivePatient()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(
                context,
                patientActive: false);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInactiveDoctor()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(
                context,
                doctorActive: false);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(prescription));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPrescriptionWithRelatedData()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(patient, doctor, consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        var result =
            await service.GetByIdAsync(prescription.Id);

        Assert.NotNull(result);
        Assert.Equal(prescription.Id, result.Id);
        Assert.NotNull(result.Patient);
        Assert.NotNull(result.Doctor);
        Assert.NotNull(result.Consultation);
        Assert.Equal(patient.Id, result.Patient.Id);
        Assert.Equal(doctor.Id, result.Doctor.Id);
        Assert.Equal(
            consultation.Id,
            result.Consultation.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        Assert.Null(await service.GetByIdAsync(0));
        Assert.Null(await service.GetByIdAsync(-1));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenPrescriptionDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        Assert.Null(await service.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetByConsultationAsync_ShouldReturnConsultationPrescriptions()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        context.Prescriptions.AddRange(
            CreatePrescription(
                patient,
                doctor,
                consultation),
            new Prescription
            {
                ConsultationId = consultation.Id,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                MedicationName = "Ibuprofen",
                Dosage = "200 mg",
                Frequency = "Twice daily",
                Duration = "3 days",
                Instructions = "Take with food.",
                PrescribedAtUtc =
                    DateTime.UtcNow.AddMinutes(1)
            });

        await context.SaveChangesAsync();

        var result =
            await service.GetByConsultationAsync(
                consultation.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(
            "Ibuprofen",
            result[0].MedicationName);
        Assert.Equal(
            "Paracetamol",
            result[1].MedicationName);
    }

    [Fact]
    public async Task GetByConsultationAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var result =
            await service.GetByConsultationAsync(0);

        Assert.Empty(result);

        result =
            await service.GetByConsultationAsync(-1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPatientAsync_ShouldReturnPatientPrescriptions()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        context.Prescriptions.Add(
            CreatePrescription(
                patient,
                doctor,
                consultation));

        await context.SaveChangesAsync();

        var result =
            await service.GetByPatientAsync(patient.Id);

        Assert.Single(result);
        Assert.Equal(
            patient.Id,
            result[0].PatientId);
    }

    [Fact]
    public async Task GetByPatientAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        Assert.Empty(
            await service.GetByPatientAsync(0));

        Assert.Empty(
            await service.GetByPatientAsync(-1));
    }

    [Fact]
    public async Task GetByDoctorAsync_ShouldReturnDoctorPrescriptions()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        context.Prescriptions.Add(
            CreatePrescription(
                patient,
                doctor,
                consultation));

        await context.SaveChangesAsync();

        var result =
            await service.GetByDoctorAsync(doctor.Id);

        Assert.Single(result);
        Assert.Equal(
            doctor.Id,
            result[0].DoctorId);
    }

    [Fact]
    public async Task GetByDoctorAsync_ShouldReturnEmptyForInvalidId()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        Assert.Empty(
            await service.GetByDoctorAsync(0));

        Assert.Empty(
            await service.GetByDoctorAsync(-1));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndNormalizePrescription()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.MedicationName =
            "  Amoxicillin  ";

        prescription.Dosage =
            "  500 mg  ";

        prescription.Frequency =
            "  Three times daily  ";

        prescription.Duration =
            "  7 days  ";

        prescription.Instructions =
            "  Complete the full course.  ";

        await service.UpdateAsync(prescription);

        var updated =
            await context.Prescriptions
                .FirstAsync(
                    p => p.Id == prescription.Id);

        Assert.Equal(
            "Amoxicillin",
            updated.MedicationName);

        Assert.Equal(
            "500 mg",
            updated.Dosage);

        Assert.Equal(
            "Three times daily",
            updated.Frequency);

        Assert.Equal(
            "7 days",
            updated.Duration);

        Assert.Equal(
            "Complete the full course.",
            updated.Instructions);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNormalizeBlankInstructionsToNull()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.Instructions = "   ";

        await service.UpdateAsync(prescription);

        var updated =
            await context.Prescriptions
                .FirstAsync(
                    p => p.Id == prescription.Id);

        Assert.Null(updated.Instructions);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectNullPrescription()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectInvalidId()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var prescription = new Prescription
        {
            Id = 0,
            ConsultationId = 1,
            PatientId = 1,
            DoctorId = 1,
            MedicationName = "Paracetamol",
            Dosage = "500 mg",
            Frequency = "Every 6 hours",
            Duration = "5 days"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(prescription));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowWhenPrescriptionDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var prescription = new Prescription
        {
            Id = 9999,
            ConsultationId = 1,
            PatientId = 1,
            DoctorId = 1,
            MedicationName = "Paracetamol",
            Dosage = "500 mg",
            Frequency = "Every 6 hours",
            Duration = "5 days"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(prescription));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMissingMedicationName()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.MedicationName = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(prescription));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMissingDosage()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.Dosage = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(prescription));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMissingFrequency()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.Frequency = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(prescription));
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMissingDuration()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.Duration = "   ";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(prescription));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventConsultationRelationshipChanges()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.ConsultationId = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(prescription));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventPatientRelationshipChanges()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.PatientId = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(prescription));
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventDoctorRelationshipChanges()
    {
        await using var context = CreateContext();

        var service = new PrescriptionService(context);

        var (patient, doctor, consultation) =
            await SeedDependenciesAsync(context);

        var prescription =
            CreatePrescription(
                patient,
                doctor,
                consultation);

        context.Prescriptions.Add(prescription);
        await context.SaveChangesAsync();

        prescription.DoctorId = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(prescription));
    }
}