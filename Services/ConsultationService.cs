using Microsoft.EntityFrameworkCore;
using SmartClinicManagementSystem.Data;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Services;

public class ConsultationService : IConsultationService
{
    private readonly ApplicationDbContext _context;

    public ConsultationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Consultation?> GetByIdAsync(int id)
    {
        return await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Appointment)
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.Prescriptions)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Consultation?> GetByAppointmentIdAsync(int appointmentId)
    {
        return await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Appointment)
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.Prescriptions)
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);
    }

    public async Task<IReadOnlyList<Consultation>> GetByPatientAsync(int patientId)
    {
        return await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Doctor)
            .Include(c => c.Appointment)
            .OrderByDescending(c => c.ConsultationDateUtc)
            .Where(c => c.PatientId == patientId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Consultation>> GetByDoctorAsync(int doctorId)
    {
        return await _context.Consultations
            .AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.Appointment)
            .OrderByDescending(c => c.ConsultationDateUtc)
            .Where(c => c.DoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<Consultation> CreateAsync(Consultation consultation)
    {
        consultation.Symptoms = consultation.Symptoms.Trim();
        consultation.Diagnosis = consultation.Diagnosis.Trim();
        consultation.TreatmentPlan = consultation.TreatmentPlan?.Trim();
        consultation.ClinicalNotes = consultation.ClinicalNotes?.Trim();
        consultation.ConsultationDateUtc = DateTime.UtcNow;
        consultation.UpdatedAtUtc = null;

        var appointmentExists = await _context.Appointments
            .AsNoTracking()
            .AnyAsync(a =>
                a.Id == consultation.AppointmentId &&
                a.PatientId == consultation.PatientId &&
                a.DoctorId == consultation.DoctorId);

        if (!appointmentExists)
        {
            throw new InvalidOperationException(
                "The consultation is not associated with a valid appointment.");
        }

        var consultationExists = await _context.Consultations
            .AsNoTracking()
            .AnyAsync(c => c.AppointmentId == consultation.AppointmentId);

        if (consultationExists)
        {
            throw new InvalidOperationException(
                "A consultation already exists for this appointment.");
        }

        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        return consultation;
    }

    public async Task UpdateAsync(Consultation consultation)
    {
        consultation.Symptoms = consultation.Symptoms.Trim();
        consultation.Diagnosis = consultation.Diagnosis.Trim();
        consultation.TreatmentPlan = consultation.TreatmentPlan?.Trim();
        consultation.ClinicalNotes = consultation.ClinicalNotes?.Trim();
        consultation.UpdatedAtUtc = DateTime.UtcNow;

        _context.Consultations.Update(consultation);
        await _context.SaveChangesAsync();
    }
}