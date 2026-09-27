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

        var patientExists =
            await _context.Patients
                .AsNoTracking()
                .AnyAsync(p =>
                    p.Id == record.PatientId &&
                    p.IsActive);

        if (!patientExists)
        {
            throw new InvalidOperationException(
                "The selected patient does not exist or is inactive.");
        }

        var doctorExists =
            await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d =>
                    d.Id == record.DoctorId &&
                    d.IsActive);

        if (!doctorExists)
        {
            throw new InvalidOperationException(
                "The selected doctor does not exist or is inactive.");
        }

        record.RecordType =
            record.RecordType.Trim();

        record.Description =
            record.Description.Trim();

        record.ClinicalNotes =
            string.IsNullOrWhiteSpace(record.ClinicalNotes)
                ? null
                : record.ClinicalNotes.Trim();

        record.RecordedAtUtc =
            record.RecordedAtUtc == default
                ? DateTime.UtcNow
                : record.RecordedAtUtc;

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
            throw new InvalidOperationException(
                "A valid medical record is required.");
        }

        var existing =
            await _context.MedicalRecords
                .FirstOrDefaultAsync(
                    m => m.Id == record.Id);

        if (existing is null)
        {
            throw new InvalidOperationException(
                "The medical record could not be found.");
        }

        if (existing.PatientId != record.PatientId ||
            existing.DoctorId != record.DoctorId)
        {
            throw new InvalidOperationException(
                "The patient and doctor relationships cannot be changed.");
        }

        if (string.IsNullOrWhiteSpace(record.RecordType))
        {
            throw new InvalidOperationException(
                "Record type is required.");
        }

        if (string.IsNullOrWhiteSpace(record.Description))
        {
            throw new InvalidOperationException(
                "Record description is required.");
        }

        existing.RecordType =
            record.RecordType.Trim();

        existing.Description =
            record.Description.Trim();

        existing.ClinicalNotes =
            string.IsNullOrWhiteSpace(record.ClinicalNotes)
                ? null
                : record.ClinicalNotes.Trim();

        await _context.SaveChangesAsync();
    }

    private static void ValidateRecord(
        MedicalRecord record)
    {
        if (record.PatientId <= 0)
        {
            throw new InvalidOperationException(
                "A valid patient is required.");
        }

        if (record.DoctorId <= 0)
        {
            throw new InvalidOperationException(
                "A valid doctor is required.");
        }

        if (string.IsNullOrWhiteSpace(record.RecordType))
        {
            throw new InvalidOperationException(
                "Record type is required.");
        }

        if (string.IsNullOrWhiteSpace(record.Description))
        {
            throw new InvalidOperationException(
                "Record description is required.");
        }
    }
}