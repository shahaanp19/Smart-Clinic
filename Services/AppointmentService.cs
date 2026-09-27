using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class AppointmentService : IAppointmentService
{
    private static readonly string[] ConflictingStatuses =
    {
        "Scheduled",
        "Confirmed"
    };

    private readonly ApplicationDbContext _context;

    public AppointmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

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
        ValidateDateRange(doctorId, fromUtc, toUtc);

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
        ValidateDateRange(patientId, fromUtc, toUtc);

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
        if (doctorId <= 0)
        {
            return false;
        }

        var query = _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDateTime == appointmentDateTimeUtc &&
                ConflictingStatuses.Contains(a.Status));

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(
                a => a.Id != excludeAppointmentId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<Appointment> CreateAsync(
        Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        ValidateReferences(appointment);

        appointment.AppointmentDateTime =
            NormalizeDateTime(appointment.AppointmentDateTime);

        if (appointment.AppointmentDateTime <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "Appointments must be scheduled for a future date and time.");
        }

        appointment.Status =
            NormalizeStatus(appointment.Status);

        appointment.Reason =
            NormalizeOptionalText(appointment.Reason);

        appointment.Notes =
            NormalizeOptionalText(appointment.Notes);

        await ValidateActivePatientAsync(
            appointment.PatientId);

        await ValidateActiveDoctorAsync(
            appointment.DoctorId);

        if (!await IsSlotAvailableAsync(
                appointment.DoctorId,
                appointment.AppointmentDateTime))
        {
            throw new InvalidOperationException(
                "The selected appointment slot is already booked.");
        }

        appointment.CreatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedAtUtc = null;

        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        return appointment;
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        if (appointment.Id <= 0)
        {
            throw new ArgumentException(
                "A valid appointment ID is required.",
                nameof(appointment));
        }

        var existing = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointment.Id);

        if (existing is null)
        {
            throw new KeyNotFoundException(
                "The appointment could not be found.");
        }

        ValidateReferences(appointment);

        var appointmentDateTime =
            NormalizeDateTime(
                appointment.AppointmentDateTime);

        var status =
            NormalizeStatus(appointment.Status);

        await ValidateActivePatientAsync(
            appointment.PatientId);

        await ValidateActiveDoctorAsync(
            appointment.DoctorId);

        if (appointmentDateTime <= DateTime.UtcNow &&
            status is not "Completed" and not "Cancelled")
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
        existing.Status = status;
        existing.Reason =
            NormalizeOptionalText(appointment.Reason);
        existing.Notes =
            NormalizeOptionalText(appointment.Notes);
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> CancelAsync(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment is null)
        {
            return false;
        }

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

    private async Task ValidateActivePatientAsync(int patientId)
    {
        var exists = await _context.Patients
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == patientId &&
                p.IsActive);

        if (!exists)
        {
            throw new InvalidOperationException(
                "The selected patient does not exist or is inactive.");
        }
    }

    private async Task ValidateActiveDoctorAsync(int doctorId)
    {
        var exists = await _context.Doctors
            .AsNoTracking()
            .AnyAsync(d =>
                d.Id == doctorId &&
                d.IsActive);

        if (!exists)
        {
            throw new InvalidOperationException(
                "The selected doctor does not exist or is inactive.");
        }
    }

    private static void ValidateReferences(
        Appointment appointment)
    {
        if (appointment.PatientId <= 0)
        {
            throw new ArgumentException(
                "A valid patient is required.",
                nameof(appointment.PatientId));
        }

        if (appointment.DoctorId <= 0)
        {
            throw new ArgumentException(
                "A valid doctor is required.",
                nameof(appointment.DoctorId));
        }
    }

    private static void ValidateDateRange(
        int entityId,
        DateTime fromUtc,
        DateTime toUtc)
    {
        if (entityId <= 0)
        {
            throw new ArgumentException(
                "A valid record ID is required.",
                nameof(entityId));
        }

        if (fromUtc > toUtc)
        {
            throw new ArgumentException(
                "The start date must be earlier than or equal to the end date.");
        }
    }

    private static DateTime NormalizeDateTime(
        DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc);
        }

        return value.ToUniversalTime();
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Scheduled";
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "scheduled" => "Scheduled",
            "confirmed" => "Confirmed",
            "completed" => "Completed",
            "cancelled" => "Cancelled",
            "no-show" => "No-Show",
            "no show" => "No-Show",
            _ => throw new ArgumentException(
                "The appointment status is invalid.",
                nameof(status))
        };
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}