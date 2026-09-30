using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class DoctorService : IDoctorService
{
    private readonly ApplicationDbContext _context;

    public DoctorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Doctor?> GetByEmployeeNumberAsync(
        string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            return null;
        }

        var normalizedNumber =
            employeeNumber.Trim().ToUpperInvariant();

        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.EmployeeNumber == normalizedNumber);
    }

    public async Task<Doctor?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail =
            email.Trim().ToLowerInvariant();

        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Email.ToLower() == normalizedEmail);
    }

    public async Task<IReadOnlyList<Doctor>> GetAllAsync()
    {
        return await _context.Doctors
            .AsNoTracking()
            .OrderBy(d => d.FullName)
            .ToListAsync();
    }

    public async Task<Doctor> CreateAsync(Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(doctor);

        ValidateDoctor(doctor);
        NormalizeDoctor(doctor);

        var duplicateEmployeeNumber =
            await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d =>
                    d.EmployeeNumber ==
                    doctor.EmployeeNumber);

        if (duplicateEmployeeNumber)
        {
            throw new InvalidOperationException(
                "A doctor with this employee number already exists.");
        }

        var duplicateEmail =
            await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d =>
                    d.Email.ToLower() ==
                    doctor.Email);

        if (duplicateEmail)
        {
            throw new InvalidOperationException(
                "A doctor with this email address already exists.");
        }

        doctor.Id = 0;
        doctor.CreatedAtUtc = DateTime.UtcNow;
        doctor.UpdatedAtUtc = null;
        doctor.IsActive = true;

        _context.Doctors.Add(doctor);

        await _context.SaveChangesAsync();

        return doctor;
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(doctor);

        if (doctor.Id <= 0)
        {
            throw new ArgumentException(
                "A valid doctor ID is required.",
                nameof(doctor));
        }

        ValidateDoctor(doctor);
        NormalizeDoctor(doctor);

        var existing = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == doctor.Id);

        if (existing is null)
        {
            throw new KeyNotFoundException(
                "The doctor could not be found.");
        }

        var duplicateEmployeeNumber =
            await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d =>
                    d.Id != doctor.Id &&
                    d.EmployeeNumber ==
                    doctor.EmployeeNumber);

        if (duplicateEmployeeNumber)
        {
            throw new InvalidOperationException(
                "A doctor with this employee number already exists.");
        }

        var duplicateEmail =
            await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d =>
                    d.Id != doctor.Id &&
                    d.Email.ToLower() ==
                    doctor.Email);

        if (duplicateEmail)
        {
            throw new InvalidOperationException(
                "A doctor with this email address already exists.");
        }

        existing.FullName = doctor.FullName;
        existing.EmployeeNumber = doctor.EmployeeNumber;
        existing.Specialisation = doctor.Specialisation;
        existing.Email = doctor.Email;
        existing.PhoneNumber = doctor.PhoneNumber;
        existing.Qualifications = doctor.Qualifications;
        existing.IsActive = doctor.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor is null)
        {
            return false;
        }

        if (!doctor.IsActive)
        {
            return true;
        }

        doctor.IsActive = false;
        doctor.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    private static void ValidateDoctor(Doctor doctor)
    {
        if (string.IsNullOrWhiteSpace(doctor.FullName))
        {
            throw new InvalidOperationException(
                "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(doctor.EmployeeNumber))
        {
            throw new InvalidOperationException(
                "Employee number is required.");
        }

        if (string.IsNullOrWhiteSpace(doctor.Specialisation))
        {
            throw new InvalidOperationException(
                "Specialisation is required.");
        }

        if (string.IsNullOrWhiteSpace(doctor.Email))
        {
            throw new InvalidOperationException(
                "Email address is required.");
        }

        if (string.IsNullOrWhiteSpace(doctor.PhoneNumber))
        {
            throw new InvalidOperationException(
                "Phone number is required.");
        }

        if (doctor.Qualifications is not null &&
            doctor.Qualifications.Length > 1000)
        {
            throw new InvalidOperationException(
                "Qualifications cannot exceed 1000 characters.");
        }
    }

    private static void NormalizeDoctor(Doctor doctor)
    {
        doctor.FullName = doctor.FullName.Trim();

        doctor.EmployeeNumber =
            doctor.EmployeeNumber
                .Trim()
                .ToUpperInvariant();

        doctor.Specialisation =
            doctor.Specialisation.Trim();

        doctor.Email =
            doctor.Email
                .Trim()
                .ToLowerInvariant();

        doctor.PhoneNumber =
            doctor.PhoneNumber.Trim();

        doctor.Qualifications =
            string.IsNullOrWhiteSpace(doctor.Qualifications)
                ? null
                : doctor.Qualifications.Trim();
    }
}