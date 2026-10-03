using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IConsultationService
{
    Task<Consultation?> GetByIdAsync(int id);

    Task<Consultation?> GetByAppointmentIdAsync(int appointmentId);

    Task<IReadOnlyList<Consultation>> GetByPatientAsync(int patientId);

    Task<IReadOnlyList<Consultation>> GetByDoctorAsync(int doctorId);

    Task<Consultation> CreateAsync(Consultation consultation);

    Task UpdateAsync(Consultation consultation);
}