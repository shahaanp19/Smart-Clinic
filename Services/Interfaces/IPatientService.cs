using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IPatientService
{
    Task<Patient?> GetByIdAsync(int id);

    Task<Patient?> GetByPatientNumberAsync(string patientNumber);

    Task<IReadOnlyList<Patient>> GetAllAsync();

    Task<Patient> CreateAsync(Patient patient);

    Task UpdateAsync(Patient patient);

    Task<bool> DeactivateAsync(int id);
}