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
        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Where(p => p.ConsultationId == consultationId)
            .OrderByDescending(p => p.PrescribedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Prescription>> GetByPatientAsync(
        int patientId)
    {
        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Doctor)
            .Include(p => p.Consultation)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.PrescribedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Prescription>> GetByDoctorAsync(
        int doctorId)
    {
        return await _context.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Consultation)
            .Where(p => p.DoctorId == doctorId)
            .OrderByDescending(p => p.PrescribedAtUtc)
            .ToListAsync();
    }

    public async Task<Prescription> CreateAsync(Prescription prescription)
    {
        prescription.MedicationName = prescription.MedicationName.Trim();
        prescription.Dosage = prescription.Dosage.Trim();
        prescription.Frequency = prescription.Frequency.Trim();
        prescription.Duration = prescription.Duration.Trim();
        prescription.Instructions = prescription.Instructions?.Trim();
        prescription.PrescribedAtUtc = DateTime.UtcNow;

        var consultation = await _context.Consultations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == prescription.ConsultationId);

        if (consultation is null)
        {
            throw new InvalidOperationException(
                "The prescription must be associated with a valid consultation.");
        }

        if (consultation.PatientId != prescription.PatientId ||
            consultation.DoctorId != prescription.DoctorId)
        {
            throw new InvalidOperationException(
                "The prescription patient and doctor must match the consultation.");
        }

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return prescription;
    }

    public async Task UpdateAsync(Prescription prescription)
    {
        prescription.MedicationName = prescription.MedicationName.Trim();
        prescription.Dosage = prescription.Dosage.Trim();
        prescription.Frequency = prescription.Frequency.Trim();
        prescription.Duration = prescription.Duration.Trim();
        prescription.Instructions = prescription.Instructions?.Trim();

        _context.Prescriptions.Update(prescription);
        await _context.SaveChangesAsync();
    }
}