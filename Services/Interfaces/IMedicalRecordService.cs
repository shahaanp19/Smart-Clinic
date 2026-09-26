using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IMedicalRecordService
{
    Task<MedicalRecord?> GetByIdAsync(int id);

    Task<IReadOnlyList<MedicalRecord>> GetByPatientAsync(int patientId);

    Task<IReadOnlyList<MedicalRecord>> GetByDoctorAsync(int doctorId);

    Task<MedicalRecord> CreateAsync(MedicalRecord medicalRecord);

    Task UpdateAsync(MedicalRecord medicalRecord);
}