using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class ConsultationService : IConsultationService
{
    private readonly ApplicationDbContext _context;

    public ConsultationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Consultation?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Appointment)
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.Prescriptions)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Consultation?> GetByAppointmentIdAsync(
        int appointmentId)
    {
        if (appointmentId <= 0)
        {
            return null;
        }

        return await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Appointment)
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.Prescriptions)
            .FirstOrDefaultAsync(
                c => c.AppointmentId == appointmentId);
    }

    public async Task<IReadOnlyList<Consultation>> GetByPatientAsync(
        int patientId)
    {
        if (patientId <= 0)
        {
            return Array.Empty<Consultation>();
        }

        return await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.Prescriptions)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.ConsultationDateUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Consultation>> GetByDoctorAsync(
        int doctorId)
    {
        if (doctorId <= 0)
        {
            return Array.Empty<Consultation>();
        }

        return await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.Prescriptions)
            .Where(c => c.DoctorId == doctorId)
            .OrderByDescending(c => c.ConsultationDateUtc)
            .ToListAsync();
    }

    public async Task<Consultation> CreateAsync(
        Consultation consultation)
    {
        ArgumentNullException.ThrowIfNull(consultation);

        ValidateRequiredReferences(consultation);
        ValidateRequiredClinicalFields(consultation);

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(
                a => a.Id == consultation.AppointmentId);

        if (appointment is null)
        {
            throw new InvalidOperationException(
                "The selected appointment could not be found.");
        }

        if (appointment.PatientId != consultation.PatientId)
        {
            throw new InvalidOperationException(
                "The consultation patient does not match the appointment.");
        }

        if (appointment.DoctorId != consultation.DoctorId)
        {
            throw new InvalidOperationException(
                "The consultation doctor does not match the appointment.");
        }

        if (string.Equals(
                appointment.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A consultation cannot be recorded for a cancelled appointment.");
        }

        if (string.Equals(
                appointment.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A consultation has already been completed for this appointment.");
        }

        if (string.Equals(
                appointment.Status,
                "No-Show",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A consultation cannot be recorded for a no-show appointment.");
        }

        await ValidateActivePatientAsync(
            consultation.PatientId);

        await ValidateActiveDoctorAsync(
            consultation.DoctorId);

        var existingConsultation =
            await _context.Consultations
                .AsNoTracking()
                .AnyAsync(c =>
                    c.AppointmentId ==
                    consultation.AppointmentId);

        if (existingConsultation)
        {
            throw new InvalidOperationException(
                "A consultation has already been recorded for this appointment.");
        }

        NormalizeConsultation(consultation);

        _context.Consultations.Add(consultation);

        appointment.Status = "Completed";
        appointment.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return consultation;
    }

    public async Task UpdateAsync(
        Consultation consultation)
    {
        ArgumentNullException.ThrowIfNull(consultation);

        if (consultation.Id <= 0)
        {
            throw new ArgumentException(
                "A valid consultation ID is required.",
                nameof(consultation));
        }

        var existing = await _context.Consultations
            .FirstOrDefaultAsync(
                c => c.Id == consultation.Id);

        if (existing is null)
        {
            throw new KeyNotFoundException(
                "The consultation could not be found.");
        }

        ValidateRequiredClinicalFields(consultation);

        if (ReferenceEquals(existing, consultation))
        {
            var originalValues =
                _context.Entry(existing).OriginalValues;

            var originalPatientId =
                originalValues.GetValue<int>(
                    nameof(Consultation.PatientId));

            var originalDoctorId =
                originalValues.GetValue<int>(
                    nameof(Consultation.DoctorId));

            var originalAppointmentId =
                originalValues.GetValue<int>(
                    nameof(Consultation.AppointmentId));

            if (originalPatientId != consultation.PatientId ||
                originalDoctorId != consultation.DoctorId ||
                originalAppointmentId != consultation.AppointmentId)
            {
                throw new InvalidOperationException(
                    "The consultation relationships cannot be changed.");
            }
        }
        else if (existing.PatientId != consultation.PatientId ||
                 existing.DoctorId != consultation.DoctorId ||
                 existing.AppointmentId != consultation.AppointmentId)
        {
            throw new InvalidOperationException(
                "The consultation relationships cannot be changed.");
        }

        NormalizeClinicalFields(consultation);

        existing.Symptoms = consultation.Symptoms;
        existing.Diagnosis = consultation.Diagnosis;
        existing.TreatmentPlan = consultation.TreatmentPlan;
        existing.ClinicalNotes = consultation.ClinicalNotes;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task ValidateActivePatientAsync(
        int patientId)
    {
        var exists = await _context.Patients
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == patientId &&
                p.IsActive);

        if (!exists)
        {
            throw new InvalidOperationException(
                "The selected patient does not exist or is inactive.");
        }
    }

    private async Task ValidateActiveDoctorAsync(
        int doctorId)
    {
        var exists = await _context.Doctors
            .AsNoTracking()
            .AnyAsync(d =>
                d.Id == doctorId &&
                d.IsActive);

        if (!exists)
        {
            throw new InvalidOperationException(
                "The selected doctor does not exist or is inactive.");
        }
    }

    private static void ValidateRequiredReferences(
        Consultation consultation)
    {
        if (consultation.AppointmentId <= 0)
        {
            throw new ArgumentException(
                "A valid appointment is required.",
                nameof(consultation.AppointmentId));
        }

        if (consultation.PatientId <= 0)
        {
            throw new ArgumentException(
                "A valid patient is required.",
                nameof(consultation.PatientId));
        }

        if (consultation.DoctorId <= 0)
        {
            throw new ArgumentException(
                "A valid doctor is required.",
                nameof(consultation.DoctorId));
        }
    }

    private static void ValidateRequiredClinicalFields(
        Consultation consultation)
    {
        if (string.IsNullOrWhiteSpace(consultation.Symptoms))
        {
            throw new InvalidOperationException(
                "Symptoms are required.");
        }

        if (string.IsNullOrWhiteSpace(consultation.Diagnosis))
        {
            throw new InvalidOperationException(
                "Diagnosis is required.");
        }
    }

    private static void NormalizeConsultation(
        Consultation consultation)
    {
        NormalizeClinicalFields(consultation);

        consultation.ConsultationDateUtc =
            consultation.ConsultationDateUtc == default
                ? DateTime.UtcNow
                : NormalizeUtc(
                    consultation.ConsultationDateUtc);

        consultation.UpdatedAtUtc = null;
    }

    private static void NormalizeClinicalFields(
        Consultation consultation)
    {
        consultation.Symptoms =
            consultation.Symptoms.Trim();

        consultation.Diagnosis =
            consultation.Diagnosis.Trim();

        consultation.TreatmentPlan =
            NormalizeOptionalText(
                consultation.TreatmentPlan);

        consultation.ClinicalNotes =
            NormalizeOptionalText(
                consultation.ClinicalNotes);
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc)
        };
    }
}