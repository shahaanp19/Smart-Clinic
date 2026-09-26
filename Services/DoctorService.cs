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
        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Doctor?> GetByEmployeeNumberAsync(string employeeNumber)
    {
        var normalizedNumber = employeeNumber.Trim().ToUpperInvariant();

        return await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.EmployeeNumber == normalizedNumber);
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
        doctor.FullName = doctor.FullName.Trim();
        doctor.EmployeeNumber = doctor.EmployeeNumber.Trim().ToUpperInvariant();
        doctor.Specialisation = doctor.Specialisation.Trim();
        doctor.Email = doctor.Email.Trim().ToLowerInvariant();
        doctor.PhoneNumber = doctor.PhoneNumber.Trim();
        doctor.Qualifications = doctor.Qualifications?.Trim();
        doctor.CreatedAtUtc = DateTime.UtcNow;
        doctor.UpdatedAtUtc = null;

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        return doctor;
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        doctor.FullName = doctor.FullName.Trim();
        doctor.EmployeeNumber = doctor.EmployeeNumber.Trim().ToUpperInvariant();
        doctor.Specialisation = doctor.Specialisation.Trim();
        doctor.Email = doctor.Email.Trim().ToLowerInvariant();
        doctor.PhoneNumber = doctor.PhoneNumber.Trim();
        doctor.Qualifications = doctor.Qualifications?.Trim();
        doctor.UpdatedAtUtc = DateTime.UtcNow;

        _context.Doctors.Update(doctor);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var doctor = await _context.Doctors
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