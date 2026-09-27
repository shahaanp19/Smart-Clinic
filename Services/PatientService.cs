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
        if (id <= 0)
        {
            return null;
        }

        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Patient?> GetByPatientNumberAsync(
        string patientNumber)
    {
        if (string.IsNullOrWhiteSpace(patientNumber))
        {
            return null;
        }

        var normalizedNumber =
            patientNumber.Trim().ToUpperInvariant();

        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.PatientNumber == normalizedNumber);
    }

    public async Task<Patient?> GetByIdNumberAsync(
        string idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return null;
        }

        var normalizedIdNumber =
            idNumber.Trim();

        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.IdNumber == normalizedIdNumber);
    }

    public async Task<Patient?> GetByEmailAsync(
        string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail =
            email.Trim().ToLowerInvariant();

        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Email != null &&
                     p.Email.ToLower() == normalizedEmail);
    }

    public async Task<IReadOnlyList<Patient>> GetAllAsync()
    {
        return await _context.Patients
            .AsNoTracking()
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<Patient> CreateAsync(
        Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);

        patient.FullName =
            patient.FullName.Trim();

        patient.PatientNumber =
            patient.PatientNumber
                .Trim()
                .ToUpperInvariant();

        patient.PhoneNumber =
            patient.PhoneNumber.Trim();

        patient.Email =
            string.IsNullOrWhiteSpace(patient.Email)
                ? null
                : patient.Email.Trim().ToLowerInvariant();

        patient.IdNumber =
            patient.IdNumber.Trim();

        patient.Gender =
            string.IsNullOrWhiteSpace(patient.Gender)
                ? null
                : patient.Gender.Trim();

        patient.Address =
            string.IsNullOrWhiteSpace(patient.Address)
                ? null
                : patient.Address.Trim();

        patient.CreatedAtUtc =
            DateTime.UtcNow;

        patient.UpdatedAtUtc = null;
        patient.IsActive = true;

        _context.Patients.Add(patient);

        await _context.SaveChangesAsync();

        return patient;
    }

    public async Task UpdateAsync(
        Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);

        patient.FullName =
            patient.FullName.Trim();

        patient.PatientNumber =
            patient.PatientNumber
                .Trim()
                .ToUpperInvariant();

        patient.PhoneNumber =
            patient.PhoneNumber.Trim();

        patient.Email =
            string.IsNullOrWhiteSpace(patient.Email)
                ? null
                : patient.Email.Trim().ToLowerInvariant();

        patient.IdNumber =
            patient.IdNumber.Trim();

        patient.Gender =
            string.IsNullOrWhiteSpace(patient.Gender)
                ? null
                : patient.Gender.Trim();

        patient.Address =
            string.IsNullOrWhiteSpace(patient.Address)
                ? null
                : patient.Address.Trim();

        patient.UpdatedAtUtc =
            DateTime.UtcNow;

        _context.Patients.Update(patient);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        var patient =
            await _context.Patients
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