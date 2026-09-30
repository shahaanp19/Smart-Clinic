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
            NormalizePatientNumber(patientNumber);

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

        var normalizedIdNumber = idNumber.Trim();

        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.IdNumber == normalizedIdNumber);
    }

    public async Task<Patient?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = NormalizeEmail(email);

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

    public async Task<IReadOnlyList<Patient>> SearchAsync(
        string? search)
    {
        var query = _context.Patients
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();

            query = query.Where(p =>
                p.FullName.Contains(normalizedSearch) ||
                p.PatientNumber.Contains(normalizedSearch) ||
                p.IdNumber.Contains(normalizedSearch) ||
                (p.Email != null &&
                 p.Email.Contains(normalizedSearch)));
        }

        return await query
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<Patient> CreateAsync(Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);

        ValidatePatient(patient);
        NormalizePatient(patient);

        await ValidateUniquePatientFieldsAsync(patient);

        patient.Id = 0;
        patient.CreatedAtUtc = DateTime.UtcNow;
        patient.UpdatedAtUtc = null;
        patient.IsActive = true;

        _context.Patients.Add(patient);

        await _context.SaveChangesAsync();

        return patient;
    }

    public async Task UpdateAsync(Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);

        if (patient.Id <= 0)
        {
            throw new ArgumentException(
                "A valid patient ID is required.",
                nameof(patient));
        }

        ValidatePatient(patient);
        NormalizePatient(patient);

        var existingPatient =
            await _context.Patients
                .FirstOrDefaultAsync(
                    p => p.Id == patient.Id);

        if (existingPatient is null)
        {
            throw new KeyNotFoundException(
                "The patient could not be found.");
        }

        await ValidateUniquePatientFieldsAsync(
            patient,
            patient.Id);

        existingPatient.FullName =
            patient.FullName;

        existingPatient.PatientNumber =
            patient.PatientNumber;

        existingPatient.PhoneNumber =
            patient.PhoneNumber;

        existingPatient.Email =
            patient.Email;

        existingPatient.IdNumber =
            patient.IdNumber;

        existingPatient.DateOfBirth =
            patient.DateOfBirth;

        existingPatient.Gender =
            patient.Gender;

        existingPatient.Address =
            patient.Address;

        existingPatient.IsActive =
            patient.IsActive;

        existingPatient.UpdatedAtUtc =
            DateTime.UtcNow;

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
                .FirstOrDefaultAsync(
                    p => p.Id == id);

        if (patient is null)
        {
            return false;
        }

        if (!patient.IsActive)
        {
            return true;
        }

        patient.IsActive = false;
        patient.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task ValidateUniquePatientFieldsAsync(
        Patient patient,
        int? excludedId = null)
    {
        var duplicatePatientNumber =
            await _context.Patients
                .AsNoTracking()
                .AnyAsync(p =>
                    (!excludedId.HasValue ||
                     p.Id != excludedId.Value) &&
                    p.PatientNumber ==
                    patient.PatientNumber);

        if (duplicatePatientNumber)
        {
            throw new InvalidOperationException(
                "A patient with this patient number already exists.");
        }

        var duplicateIdNumber =
            await _context.Patients
                .AsNoTracking()
                .AnyAsync(p =>
                    (!excludedId.HasValue ||
                     p.Id != excludedId.Value) &&
                    p.IdNumber ==
                    patient.IdNumber);

        if (duplicateIdNumber)
        {
            throw new InvalidOperationException(
                "A patient with this ID number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(patient.Email))
        {
            var duplicateEmail =
                await _context.Patients
                    .AsNoTracking()
                    .AnyAsync(p =>
                        (!excludedId.HasValue ||
                         p.Id != excludedId.Value) &&
                        p.Email != null &&
                        p.Email.ToLower() ==
                        patient.Email);

            if (duplicateEmail)
            {
                throw new InvalidOperationException(
                    "A patient with this email address already exists.");
            }
        }
    }

    private static void ValidatePatient(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.FullName))
        {
            throw new InvalidOperationException(
                "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(patient.PatientNumber))
        {
            throw new InvalidOperationException(
                "Patient number is required.");
        }

        if (string.IsNullOrWhiteSpace(patient.PhoneNumber))
        {
            throw new InvalidOperationException(
                "Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(patient.IdNumber))
        {
            throw new InvalidOperationException(
                "ID number is required.");
        }

        if (patient.DateOfBirth == default)
        {
            throw new InvalidOperationException(
                "Date of birth is required.");
        }

        if (patient.DateOfBirth.Date > DateTime.UtcNow.Date)
        {
            throw new InvalidOperationException(
                "Date of birth cannot be in the future.");
        }
    }

    private static void NormalizePatient(Patient patient)
    {
        patient.FullName =
            patient.FullName.Trim();

        patient.PatientNumber =
            NormalizePatientNumber(
                patient.PatientNumber);

        patient.PhoneNumber =
            patient.PhoneNumber.Trim();

        patient.Email =
            string.IsNullOrWhiteSpace(patient.Email)
                ? null
                : NormalizeEmail(patient.Email);

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

        patient.DateOfBirth =
            patient.DateOfBirth.Date;
    }

    private static string NormalizePatientNumber(
        string patientNumber)
    {
        return patientNumber
            .Trim()
            .ToUpperInvariant();
    }

    private static string NormalizeEmail(string email)
    {
        return email
            .Trim()
            .ToLowerInvariant();
    }
}