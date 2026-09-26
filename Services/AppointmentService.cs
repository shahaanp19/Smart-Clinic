using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;

    public AppointmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IReadOnlyList<Appointment>> GetAllAsync()
    {
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Appointment>> GetByDoctorAsync(
        int doctorId,
        DateTime fromUtc,
        DateTime toUtc)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDateTime >= fromUtc &&
                a.AppointmentDateTime <= toUtc)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Appointment>> GetByPatientAsync(
        int patientId,
        DateTime fromUtc,
        DateTime toUtc)
    {
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a =>
                a.PatientId == patientId &&
                a.AppointmentDateTime >= fromUtc &&
                a.AppointmentDateTime <= toUtc)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task<bool> IsSlotAvailableAsync(
        int doctorId,
        DateTime appointmentDateTimeUtc,
        int? excludeAppointmentId = null)
    {
        return !await _context.Appointments
            .AsNoTracking()
            .AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDateTime == appointmentDateTimeUtc &&
                a.Status != "Cancelled" &&
                (!excludeAppointmentId.HasValue ||
                 a.Id != excludeAppointmentId.Value));
    }

    public async Task<Appointment> CreateAsync(Appointment appointment)
    {
        appointment.Status = string.IsNullOrWhiteSpace(appointment.Status)
            ? "Scheduled"
            : appointment.Status.Trim();

        appointment.Reason = appointment.Reason?.Trim();
        appointment.Notes = appointment.Notes?.Trim();
        appointment.CreatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedAtUtc = null;

        if (!await IsSlotAvailableAsync(
                appointment.DoctorId,
                appointment.AppointmentDateTime))
        {
            throw new InvalidOperationException(
                "The selected appointment slot is already booked.");
        }

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return appointment;
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        appointment.Status = appointment.Status.Trim();
        appointment.Reason = appointment.Reason?.Trim();
        appointment.Notes = appointment.Notes?.Trim();
        appointment.UpdatedAtUtc = DateTime.UtcNow;

        if (!await IsSlotAvailableAsync(
                appointment.DoctorId,
                appointment.AppointmentDateTime,
                appointment.Id))
        {
            throw new InvalidOperationException(
                "The selected appointment slot is already booked.");
        }

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CancelAsync(int id)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment is null)
        {
            return false;
        }

        appointment.Status = "Cancelled";
        appointment.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}