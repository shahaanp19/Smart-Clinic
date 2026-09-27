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
        if (fromUtc > toUtc)
            throw new ArgumentException(
                "The start date must be earlier than the end date.");

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
        if (fromUtc > toUtc)
            throw new ArgumentException(
                "The start date must be earlier than the end date.");

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
        var conflictingStatuses = new[]
        {
            "Scheduled",
            "Confirmed"
        };

        var query = _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDateTime == appointmentDateTimeUtc &&
                conflictingStatuses.Contains(a.Status));

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(
                a => a.Id != excludeAppointmentId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<Appointment> CreateAsync(Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        if (appointment.PatientId <= 0)
            throw new ArgumentException(
                "A valid patient is required.");

        if (appointment.DoctorId <= 0)
            throw new ArgumentException(
                "A valid doctor is required.");

        if (appointment.AppointmentDateTime.Kind == DateTimeKind.Unspecified)
        {
            appointment.AppointmentDateTime =
                DateTime.SpecifyKind(
                    appointment.AppointmentDateTime,
                    DateTimeKind.Utc);
        }
        else
        {
            appointment.AppointmentDateTime =
                appointment.AppointmentDateTime.ToUniversalTime();
        }

        if (appointment.AppointmentDateTime <= DateTime.UtcNow)
            throw new InvalidOperationException(
                "Appointments must be scheduled for a future date and time.");

        var patientExists = await _context.Patients
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == appointment.PatientId &&
                p.IsActive);

        if (!patientExists)
            throw new InvalidOperationException(
                "The selected patient does not exist or is inactive.");

        var doctorExists = await _context.Doctors
            .AsNoTracking()
            .AnyAsync(d =>
                d.Id == appointment.DoctorId &&
                d.IsActive);

        if (!doctorExists)
            throw new InvalidOperationException(
                "The selected doctor does not exist or is inactive.");

        if (!await IsSlotAvailableAsync(
                appointment.DoctorId,
                appointment.AppointmentDateTime))
        {
            throw new InvalidOperationException(
                "The selected appointment slot is already booked.");
        }

        appointment.Status = NormalizeStatus(appointment.Status);
        appointment.Reason = NormalizeOptionalText(appointment.Reason);
        appointment.Notes = NormalizeOptionalText(appointment.Notes);
        appointment.CreatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedAtUtc = null;

        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        return appointment;
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var existing = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);

        if (existing is null)
            throw new KeyNotFoundException(
                "The appointment could not be found.");

        if (appointment.PatientId <= 0 ||
            appointment.DoctorId <= 0)
        {
            throw new ArgumentException(
                "A valid patient and doctor are required.");
        }

        var patientExists = await _context.Patients
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == appointment.PatientId &&
                p.IsActive);

        if (!patientExists)
            throw new InvalidOperationException(
                "The selected patient does not exist or is inactive.");

        var doctorExists = await _context.Doctors
            .AsNoTracking()
            .AnyAsync(d =>
                d.Id == appointment.DoctorId &&
                d.IsActive);

        if (!doctorExists)
            throw new InvalidOperationException(
                "The selected doctor does not exist or is inactive.");

        var appointmentDateTime = appointment.AppointmentDateTime;

        if (appointmentDateTime.Kind == DateTimeKind.Unspecified)
        {
            appointmentDateTime =
                DateTime.SpecifyKind(
                    appointmentDateTime,
                    DateTimeKind.Utc);
        }
        else
        {
            appointmentDateTime =
                appointmentDateTime.ToUniversalTime();
        }

        if (appointmentDateTime <= DateTime.UtcNow &&
            !string.Equals(
                appointment.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                appointment.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "An active appointment must be scheduled for a future date and time.");
        }

        if (!await IsSlotAvailableAsync(
                appointment.DoctorId,
                appointmentDateTime,
                appointment.Id))
        {
            throw new InvalidOperationException(
                "The selected appointment slot is already booked.");
        }

        existing.PatientId = appointment.PatientId;
        existing.DoctorId = appointment.DoctorId;
        existing.AppointmentDateTime = appointmentDateTime;
        existing.Status = NormalizeStatus(appointment.Status);
        existing.Reason = NormalizeOptionalText(appointment.Reason);
        existing.Notes = NormalizeOptionalText(appointment.Notes);
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> CancelAsync(int id)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment is null)
            return false;

        if (string.Equals(
                appointment.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(
                appointment.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A completed appointment cannot be cancelled.");
        }

        appointment.Status = "Cancelled";
        appointment.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "Scheduled";

        var normalized = status.Trim();

        return normalized.ToLowerInvariant() switch
        {
            "scheduled" => "Scheduled",
            "confirmed" => "Confirmed",
            "completed" => "Completed",
            "cancelled" => "Cancelled",
            "no-show" => "No-Show",
            "no show" => "No-Show",
            _ => throw new ArgumentException(
                "The appointment status is invalid.")
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}