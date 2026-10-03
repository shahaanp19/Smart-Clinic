using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IDoctorService
{
    Task<Doctor?> GetByIdAsync(int id);
    Task<Doctor?> GetByEmployeeNumberAsync(string employeeNumber);
    Task<Doctor?> GetByEmailAsync(string email);
    Task<IReadOnlyList<Doctor>> GetAllAsync();
    Task<Doctor> CreateAsync(Doctor doctor);
    Task UpdateAsync(Doctor doctor);
    Task<bool> DeactivateAsync(int id);
}