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
        return await _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(
        int patientId)
    {
        return await _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Doctor)
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.RecordedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MedicalRecord>> GetByDoctorAsync(
        int doctorId)
    {
        return await _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Patient)
            .Where(m => m.DoctorId == doctorId)
            .OrderByDescending(m => m.RecordedAtUtc)
            .ToListAsync();
    }

    public async Task<MedicalRecord> CreateAsync(
        MedicalRecord medicalRecord)
    {
        medicalRecord.RecordType = medicalRecord.RecordType.Trim();
        medicalRecord.Description = medicalRecord.Description.Trim();
        medicalRecord.ClinicalNotes = medicalRecord.ClinicalNotes?.Trim();
        medicalRecord.RecordedAtUtc = DateTime.UtcNow;

        var patientExists = await _context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == medicalRecord.PatientId);

        if (!patientExists)
        {
            throw new InvalidOperationException(
                "The medical record must reference a valid patient.");
        }

        var doctorExists = await _context.Doctors
            .AsNoTracking()
            .AnyAsync(d => d.Id == medicalRecord.DoctorId);

        if (!doctorExists)
        {
            throw new InvalidOperationException(
                "The medical record must reference a valid doctor.");
        }

        _context.MedicalRecords.Add(medicalRecord);
        await _context.SaveChangesAsync();

        return medicalRecord;
    }

    public async Task UpdateAsync(MedicalRecord medicalRecord)
    {
        medicalRecord.RecordType = medicalRecord.RecordType.Trim();
        medicalRecord.Description = medicalRecord.Description.Trim();
        medicalRecord.ClinicalNotes = medicalRecord.ClinicalNotes?.Trim();

        _context.MedicalRecords.Update(medicalRecord);
        await _context.SaveChangesAsync();
    }
}