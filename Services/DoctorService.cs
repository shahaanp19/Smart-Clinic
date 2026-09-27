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

    public async Task<Doctor?> GetByEmailAsync(
        string email)
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

    public async Task<Doctor> CreateAsync(
        Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(doctor);

        doctor.FullName =
            doctor.FullName.Trim();

        doctor.EmployeeNumber =
            doctor.EmployeeNumber
                .Trim()
                .ToUpperInvariant();

        doctor.Specialisation =
            doctor.Specialisation.Trim();

        doctor.Email =
            doctor.Email.Trim().ToLowerInvariant();

        doctor.PhoneNumber =
            doctor.PhoneNumber.Trim();

        doctor.Qualifications =
            string.IsNullOrWhiteSpace(doctor.Qualifications)
                ? null
                : doctor.Qualifications.Trim();

        doctor.CreatedAtUtc =
            DateTime.UtcNow;

        doctor.UpdatedAtUtc = null;
        doctor.IsActive = true;

        _context.Doctors.Add(doctor);

        await _context.SaveChangesAsync();

        return doctor;
    }

    public async Task UpdateAsync(
        Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(doctor);

        var existing =
            await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == doctor.Id);

        if (existing is null)
        {
            throw new InvalidOperationException(
                "The doctor could not be found.");
        }

        existing.FullName =
            doctor.FullName.Trim();

        existing.EmployeeNumber =
            doctor.EmployeeNumber
                .Trim()
                .ToUpperInvariant();

        existing.Specialisation =
            doctor.Specialisation.Trim();

        existing.Email =
            doctor.Email.Trim().ToLowerInvariant();

        existing.PhoneNumber =
            doctor.PhoneNumber.Trim();

        existing.Qualifications =
            string.IsNullOrWhiteSpace(doctor.Qualifications)
                ? null
                : doctor.Qualifications.Trim();

        existing.IsActive =
            doctor.IsActive;

        existing.UpdatedAtUtc =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        var doctor =
            await _context.Doctors
                .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor is null)
        {
            return false;
        }

        doctor.IsActive = false;
        doctor.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}