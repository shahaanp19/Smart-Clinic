using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IPatientService
{
    Task<Patient?> GetByIdAsync(int id);
    Task<Patient?> GetByPatientNumberAsync(string patientNumber);
    Task<Patient?> GetByIdNumberAsync(string idNumber);
    Task<Patient?> GetByEmailAsync(string email);

    Task<IReadOnlyList<Patient>> GetAllAsync();

    Task<IReadOnlyList<Patient>> SearchAsync(string? search);

    Task<Patient> CreateAsync(Patient patient);
    Task UpdateAsync(Patient patient);
    Task<bool> DeactivateAsync(int id);
}