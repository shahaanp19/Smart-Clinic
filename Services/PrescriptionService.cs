using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly ApplicationDbContext _context;

    public PrescriptionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Prescription?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Consultation)
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Prescription>> GetByConsultationAsync(
        int consultationId)
    {
        if (consultationId <= 0)
        {
            return Array.Empty<Prescription>();
        }

        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Consultation)
            .Where(p => p.ConsultationId == consultationId)
            .OrderByDescending(p => p.PrescribedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Prescription>> GetByPatientAsync(
        int patientId)
    {
        if (patientId <= 0)
        {
            return Array.Empty<Prescription>();
        }

        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Consultation)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.PrescribedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Prescription>> GetByDoctorAsync(
        int doctorId)
    {
        if (doctorId <= 0)
        {
            return Array.Empty<Prescription>();
        }

        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Consultation)
            .Where(p => p.DoctorId == doctorId)
            .OrderByDescending(p => p.PrescribedAtUtc)
            .ToListAsync();
    }

    public async Task<Prescription> CreateAsync(
        Prescription prescription)
    {
        ArgumentNullException.ThrowIfNull(prescription);

        if (prescription.ConsultationId <= 0)
        {
            throw new InvalidOperationException(
                "A valid consultation is required.");
        }

        if (prescription.PatientId <= 0)
        {
            throw new InvalidOperationException(
                "A valid patient is required.");
        }

        if (prescription.DoctorId <= 0)
        {
            throw new InvalidOperationException(
                "A valid doctor is required.");
        }

        if (string.IsNullOrWhiteSpace(prescription.MedicationName))
        {
            throw new InvalidOperationException(
                "Medication name is required.");
        }

        if (string.IsNullOrWhiteSpace(prescription.Dosage))
        {
            throw new InvalidOperationException(
                "Dosage is required.");
        }

        if (string.IsNullOrWhiteSpace(prescription.Frequency))
        {
            throw new InvalidOperationException(
                "Frequency is required.");
        }

        if (string.IsNullOrWhiteSpace(prescription.Duration))
        {
            throw new InvalidOperationException(
                "Duration is required.");
        }

        var consultation =
            await _context.Consultations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.Id == prescription.ConsultationId);

        if (consultation is null)
        {
            throw new InvalidOperationException(
                "The selected consultation could not be found.");
        }

        if (consultation.PatientId != prescription.PatientId)
        {
            throw new InvalidOperationException(
                "The prescription patient does not match the consultation.");
        }

        if (consultation.DoctorId != prescription.DoctorId)
        {
            throw new InvalidOperationException(
                "The prescription doctor does not match the consultation.");
        }

        var doctorIsActive =
            await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d =>
                    d.Id == prescription.DoctorId &&
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
                    p.Id == prescription.PatientId &&
                    p.IsActive);

        if (!patientIsActive)
        {
            throw new InvalidOperationException(
                "The selected patient is not active.");
        }

        prescription.MedicationName =
            prescription.MedicationName.Trim();

        prescription.Dosage =
            prescription.Dosage.Trim();

        prescription.Frequency =
            prescription.Frequency.Trim();

        prescription.Duration =
            prescription.Duration.Trim();

        prescription.Instructions =
            string.IsNullOrWhiteSpace(prescription.Instructions)
                ? null
                : prescription.Instructions.Trim();

        prescription.PrescribedAtUtc =
            prescription.PrescribedAtUtc == default
                ? DateTime.UtcNow
                : prescription.PrescribedAtUtc;

        _context.Prescriptions.Add(prescription);

        await _context.SaveChangesAsync();

        return prescription;
    }

    public async Task UpdateAsync(
        Prescription prescription)
    {
        ArgumentNullException.ThrowIfNull(prescription);

        var existing =
            await _context.Prescriptions
                .FirstOrDefaultAsync(
                    p => p.Id == prescription.Id);

        if (existing is null)
        {
            throw new InvalidOperationException(
                "The prescription could not be found.");
        }

        if (existing.ConsultationId != prescription.ConsultationId ||
            existing.PatientId != prescription.PatientId ||
            existing.DoctorId != prescription.DoctorId)
        {
            throw new InvalidOperationException(
                "The prescription relationships cannot be changed.");
        }

        if (string.IsNullOrWhiteSpace(prescription.MedicationName))
        {
            throw new InvalidOperationException(
                "Medication name is required.");
        }

        if (string.IsNullOrWhiteSpace(prescription.Dosage))
        {
            throw new InvalidOperationException(
                "Dosage is required.");
        }

        if (string.IsNullOrWhiteSpace(prescription.Frequency))
        {
            throw new InvalidOperationException(
                "Frequency is required.");
        }

        if (string.IsNullOrWhiteSpace(prescription.Duration))
        {
            throw new InvalidOperationException(
                "Duration is required.");
        }

        existing.MedicationName =
            prescription.MedicationName.Trim();

        existing.Dosage =
            prescription.Dosage.Trim();

        existing.Frequency =
            prescription.Frequency.Trim();

        existing.Duration =
            prescription.Duration.Trim();

        existing.Instructions =
            string.IsNullOrWhiteSpace(prescription.Instructions)
                ? null
                : prescription.Instructions.Trim();

        await _context.SaveChangesAsync();
    }
}