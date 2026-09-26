using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context;

    public PatientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Patient?> GetByPatientNumberAsync(string patientNumber)
    {
        var normalizedNumber = patientNumber.Trim();

        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientNumber == normalizedNumber);
    }

    public async Task<IReadOnlyList<Patient>> GetAllAsync()
    {
        return await _context.Patients
            .AsNoTracking()
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<Patient> CreateAsync(Patient patient)
    {
        patient.FullName = patient.FullName.Trim();
        patient.PatientNumber = patient.PatientNumber.Trim().ToUpperInvariant();
        patient.PhoneNumber = patient.PhoneNumber.Trim();
        patient.Email = patient.Email?.Trim().ToLowerInvariant();
        patient.IdNumber = patient.IdNumber.Trim();
        patient.Address = patient.Address?.Trim();
        patient.CreatedAtUtc = DateTime.UtcNow;
        patient.UpdatedAtUtc = null;

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return patient;
    }

    public async Task UpdateAsync(Patient patient)
    {
        patient.FullName = patient.FullName.Trim();
        patient.PatientNumber = patient.PatientNumber.Trim().ToUpperInvariant();
        patient.PhoneNumber = patient.PhoneNumber.Trim();
        patient.Email = patient.Email?.Trim().ToLowerInvariant();
        patient.IdNumber = patient.IdNumber.Trim();
        patient.Address = patient.Address?.Trim();
        patient.UpdatedAtUtc = DateTime.UtcNow;

        _context.Patients.Update(patient);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null)
        {
            return false;
        }

        patient.IsActive = false;
        patient.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}