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

        if (consultation.AppointmentId <= 0)
        {
            throw new InvalidOperationException(
                "A valid appointment is required.");
        }

        if (consultation.PatientId <= 0)
        {
            throw new InvalidOperationException(
                "A valid patient is required.");
        }

        if (consultation.DoctorId <= 0)
        {
            throw new InvalidOperationException(
                "A valid doctor is required.");
        }

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

        var appointment =
            await _context.Appointments
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

        var doctorIsActive =
            await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d =>
                    d.Id == consultation.DoctorId &&
                    d.IsActive);

        if (!doctorIsActive)
        {
            throw new InvalidOperationException(
                "The selected doctor is not active.");
        }

        var patientIsActive =
            await _context.Patients
                .AsNoTracking()
                .AnyAsync(p =>
                    p.Id == consultation.PatientId &&
                    p.IsActive);

        if (!patientIsActive)
        {
            throw new InvalidOperationException(
                "The selected patient is not active.");
        }

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

        consultation.Symptoms =
            consultation.Symptoms.Trim();

        consultation.Diagnosis =
            consultation.Diagnosis.Trim();

        consultation.TreatmentPlan =
            string.IsNullOrWhiteSpace(consultation.TreatmentPlan)
                ? null
                : consultation.TreatmentPlan.Trim();

        consultation.ClinicalNotes =
            string.IsNullOrWhiteSpace(consultation.ClinicalNotes)
                ? null
                : consultation.ClinicalNotes.Trim();

        consultation.ConsultationDateUtc =
            consultation.ConsultationDateUtc == default
                ? DateTime.UtcNow
                : consultation.ConsultationDateUtc;

        consultation.UpdatedAtUtc = null;

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

        var existing =
            await _context.Consultations
                .FirstOrDefaultAsync(
                    c => c.Id == consultation.Id);

        if (existing is null)
        {
            throw new InvalidOperationException(
                "The consultation could not be found.");
        }

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

        if (existing.PatientId != consultation.PatientId ||
            existing.DoctorId != consultation.DoctorId ||
            existing.AppointmentId != consultation.AppointmentId)
        {
            throw new InvalidOperationException(
                "The consultation relationships cannot be changed.");
        }

        existing.Symptoms =
            consultation.Symptoms.Trim();

        existing.Diagnosis =
            consultation.Diagnosis.Trim();

        existing.TreatmentPlan =
            string.IsNullOrWhiteSpace(consultation.TreatmentPlan)
                ? null
                : consultation.TreatmentPlan.Trim();

        existing.ClinicalNotes =
            string.IsNullOrWhiteSpace(consultation.ClinicalNotes)
                ? null
                : consultation.ClinicalNotes.Trim();

        existing.UpdatedAtUtc =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}