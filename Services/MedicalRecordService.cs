using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly ApplicationDbContext _context;

    public MedicalRecordService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalRecord?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(
        int patientId)
    {
        if (patientId <= 0)
        {
            return Array.Empty<MedicalRecord>();
        }

        return await _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.RecordedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MedicalRecord>> GetByDoctorAsync(
        int doctorId)
    {
        if (doctorId <= 0)
        {
            return Array.Empty<MedicalRecord>();
        }

        return await _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Where(m => m.DoctorId == doctorId)
            .OrderByDescending(m => m.RecordedAtUtc)
            .ToListAsync();
    }

    public async Task<MedicalRecord> CreateAsync(
        MedicalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        ValidateRecord(record);
        NormalizeRecord(record);

        await ValidateActivePatientAsync(
            record.PatientId);

        await ValidateActiveDoctorAsync(
            record.DoctorId);

        record.Id = 0;

        _context.MedicalRecords.Add(record);

        await _context.SaveChangesAsync();

        return record;
    }

    public async Task UpdateAsync(
        MedicalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Id <= 0)
        {
            throw new ArgumentException(
                "A valid medical record ID is required.",
                nameof(record));
        }

        var existing =
            await _context.MedicalRecords
                .FirstOrDefaultAsync(
                    m => m.Id == record.Id);

        if (existing is null)
        {
            throw new KeyNotFoundException(
                "The medical record could not be found.");
        }

        ValidateRecordContent(record);

        if (existing.PatientId != record.PatientId ||
            existing.DoctorId != record.DoctorId)
        {
            throw new InvalidOperationException(
                "The patient and doctor relationships cannot be changed.");
        }

        await ValidateActivePatientAsync(
            existing.PatientId);

        await ValidateActiveDoctorAsync(
            existing.DoctorId);

        NormalizeRecordContent(record);

        existing.RecordType =
            record.RecordType;

        existing.Description =
            record.Description;

        existing.ClinicalNotes =
            record.ClinicalNotes;

        await _context.SaveChangesAsync();
    }

    private async Task ValidateActivePatientAsync(
        int patientId)
    {
        var exists =
            await _context.Patients
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
        var exists =
            await _context.Doctors
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

    private static void ValidateRecord(
        MedicalRecord record)
    {
        if (record.PatientId <= 0)
        {
            throw new ArgumentException(
                "A valid patient is required.",
                nameof(record.PatientId));
        }

        if (record.DoctorId <= 0)
        {
            throw new ArgumentException(
                "A valid doctor is required.",
                nameof(record.DoctorId));
        }

        ValidateRecordContent(record);
    }

    private static void ValidateRecordContent(
        MedicalRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.RecordType))
        {
            throw new InvalidOperationException(
                "Record type is required.");
        }

        if (record.RecordType.Trim().Length > 2000)
        {
            throw new InvalidOperationException(
                "Record type cannot exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(record.Description))
        {
            throw new InvalidOperationException(
                "Record description is required.");
        }

        if (record.Description.Trim().Length > 4000)
        {
            throw new InvalidOperationException(
                "Record description cannot exceed 4000 characters.");
        }

        if (record.ClinicalNotes is not null &&
            record.ClinicalNotes.Length > 2000)
        {
            throw new InvalidOperationException(
                "Clinical notes cannot exceed 2000 characters.");
        }
    }

    private static void NormalizeRecord(
        MedicalRecord record)
    {
        NormalizeRecordContent(record);

        record.RecordedAtUtc =
            record.RecordedAtUtc == default
                ? DateTime.UtcNow
                : NormalizeUtc(record.RecordedAtUtc);

        if (record.RecordedAtUtc > DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "A medical record cannot be dated in the future.");
        }
    }

    private static void NormalizeRecordContent(
        MedicalRecord record)
    {
        record.RecordType =
            record.RecordType.Trim();

        record.Description =
            record.Description.Trim();

        record.ClinicalNotes =
            string.IsNullOrWhiteSpace(record.ClinicalNotes)
                ? null
                : record.ClinicalNotes.Trim();
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