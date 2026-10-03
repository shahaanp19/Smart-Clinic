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

        ValidateRequiredReferences(prescription);
        ValidateRequiredMedicationFields(prescription);

        var consultation = await _context.Consultations
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

        await ValidateActiveDoctorAsync(
            prescription.DoctorId);

        await ValidateActivePatientAsync(
            prescription.PatientId);

        NormalizePrescription(prescription);

        _context.Prescriptions.Add(prescription);

        await _context.SaveChangesAsync();

        return prescription;
    }

    public async Task UpdateAsync(
        Prescription prescription)
    {
        ArgumentNullException.ThrowIfNull(prescription);

        if (prescription.Id <= 0)
        {
            throw new ArgumentException(
                "A valid prescription ID is required.",
                nameof(prescription));
        }

        var existing = await _context.Prescriptions
            .FirstOrDefaultAsync(
                p => p.Id == prescription.Id);

        if (existing is null)
        {
            throw new KeyNotFoundException(
                "The prescription could not be found.");
        }

        ValidateRequiredMedicationFields(prescription);

        if (ReferenceEquals(existing, prescription))
        {
            var originalValues =
                _context.Entry(existing).OriginalValues;

            var originalConsultationId =
                originalValues.GetValue<int>(
                    nameof(Prescription.ConsultationId));

            var originalPatientId =
                originalValues.GetValue<int>(
                    nameof(Prescription.PatientId));

            var originalDoctorId =
                originalValues.GetValue<int>(
                    nameof(Prescription.DoctorId));

            if (originalConsultationId !=
                    prescription.ConsultationId ||
                originalPatientId != prescription.PatientId ||
                originalDoctorId != prescription.DoctorId)
            {
                throw new InvalidOperationException(
                    "The prescription relationships cannot be changed.");
            }
        }
        else if (existing.ConsultationId !=
                     prescription.ConsultationId ||
                 existing.PatientId != prescription.PatientId ||
                 existing.DoctorId != prescription.DoctorId)
        {
            throw new InvalidOperationException(
                "The prescription relationships cannot be changed.");
        }

        NormalizeMedicationFields(prescription);

        existing.MedicationName =
            prescription.MedicationName;

        existing.Dosage =
            prescription.Dosage;

        existing.Frequency =
            prescription.Frequency;

        existing.Duration =
            prescription.Duration;

        existing.Instructions =
            prescription.Instructions;

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
        Prescription prescription)
    {
        if (prescription.ConsultationId <= 0)
        {
            throw new ArgumentException(
                "A valid consultation is required.",
                nameof(prescription.ConsultationId));
        }

        if (prescription.PatientId <= 0)
        {
            throw new ArgumentException(
                "A valid patient is required.",
                nameof(prescription.PatientId));
        }

        if (prescription.DoctorId <= 0)
        {
            throw new ArgumentException(
                "A valid doctor is required.",
                nameof(prescription.DoctorId));
        }
    }

    private static void ValidateRequiredMedicationFields(
        Prescription prescription)
    {
        if (string.IsNullOrWhiteSpace(
                prescription.MedicationName))
        {
            throw new InvalidOperationException(
                "Medication name is required.");
        }

        if (string.IsNullOrWhiteSpace(
                prescription.Dosage))
        {
            throw new InvalidOperationException(
                "Dosage is required.");
        }

        if (string.IsNullOrWhiteSpace(
                prescription.Frequency))
        {
            throw new InvalidOperationException(
                "Frequency is required.");
        }

        if (string.IsNullOrWhiteSpace(
                prescription.Duration))
        {
            throw new InvalidOperationException(
                "Duration is required.");
        }
    }

    private static void NormalizePrescription(
        Prescription prescription)
    {
        NormalizeMedicationFields(prescription);

        prescription.PrescribedAtUtc =
            prescription.PrescribedAtUtc == default
                ? DateTime.UtcNow
                : NormalizeUtc(
                    prescription.PrescribedAtUtc);
    }

    private static void NormalizeMedicationFields(
        Prescription prescription)
    {
        prescription.MedicationName =
            prescription.MedicationName.Trim();

        prescription.Dosage =
            prescription.Dosage.Trim();

        prescription.Frequency =
            prescription.Frequency.Trim();

        prescription.Duration =
            prescription.Duration.Trim();

        prescription.Instructions =
            string.IsNullOrWhiteSpace(
                prescription.Instructions)
                ? null
                : prescription.Instructions.Trim();
    }

    private static DateTime NormalizeUtc(
        DateTime value)
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