using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IPrescriptionService
{
    Task<Prescription?> GetByIdAsync(int id);

    Task<IReadOnlyList<Prescription>> GetByConsultationAsync(int consultationId);

    Task<IReadOnlyList<Prescription>> GetByPatientAsync(int patientId);

    Task<IReadOnlyList<Prescription>> GetByDoctorAsync(int doctorId);

    Task<Prescription> CreateAsync(Prescription prescription);

    Task UpdateAsync(Prescription prescription);
}