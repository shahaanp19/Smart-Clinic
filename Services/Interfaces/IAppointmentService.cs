using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IAppointmentService
{
    Task<Appointment?> GetByIdAsync(int id);

    Task<IReadOnlyList<Appointment>> GetAllAsync();

    Task<IReadOnlyList<Appointment>> GetByDoctorAsync(
        int doctorId,
        DateTime fromUtc,
        DateTime toUtc);

    Task<IReadOnlyList<Appointment>> GetByPatientAsync(
        int patientId,
        DateTime fromUtc,
        DateTime toUtc);

    Task<bool> IsSlotAvailableAsync(
        int doctorId,
        DateTime appointmentDateTimeUtc,
        int? excludeAppointmentId = null);

    Task<Appointment> CreateAsync(Appointment appointment);

    Task UpdateAsync(Appointment appointment);

    Task<bool> CancelAsync(int id);
}