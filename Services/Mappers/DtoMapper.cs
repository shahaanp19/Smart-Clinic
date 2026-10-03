using SmartClinicManagementSystem.DTOs;
using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Mappers;

public static class DtoMapper
{
    public static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive
        };
    }

    public static PatientDto ToDto(Patient patient)
    {
        return new PatientDto
        {
            Id = patient.Id,
            FullName = patient.FullName,
            PatientNumber = patient.PatientNumber,
            PhoneNumber = patient.PhoneNumber,
            Email = patient.Email,
            IdNumber = patient.IdNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Address = patient.Address,
            IsActive = patient.IsActive
        };
    }

    public static PatientSearchDto ToSearchDto(Patient patient)
    {
        return new PatientSearchDto
        {
            Id = patient.Id,
            FullName = patient.FullName,
            PatientNumber = patient.PatientNumber
        };
    }

    public static DoctorDto ToDto(Doctor doctor)
    {
        return new DoctorDto
        {
            Id = doctor.Id,
            FullName = doctor.FullName,
            EmployeeNumber = doctor.EmployeeNumber,
            Specialisation = doctor.Specialisation,
            Email = doctor.Email,
            PhoneNumber = doctor.PhoneNumber,
            Qualifications = doctor.Qualifications,
            IsActive = doctor.IsActive
        };
    }

    public static AppointmentDto ToDto(Appointment appointment)
    {
        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            AppointmentDateTime = appointment.AppointmentDateTime,
            Status = appointment.Status,
            Reason = appointment.Reason,
            Notes = appointment.Notes,
            PatientName = appointment.Patient?.FullName,
            DoctorName = appointment.Doctor?.FullName
        };
    }

    public static ConsultationDto ToDto(Consultation consultation)
    {
        return new ConsultationDto
        {
            Id = consultation.Id,
            AppointmentId = consultation.AppointmentId,
            PatientId = consultation.PatientId,
            DoctorId = consultation.DoctorId,
            Symptoms = consultation.Symptoms,
            Diagnosis = consultation.Diagnosis,
            TreatmentPlan = consultation.TreatmentPlan,
            ClinicalNotes = consultation.ClinicalNotes,
            ConsultationDateUtc = consultation.ConsultationDateUtc,
            PatientName = consultation.Patient?.FullName,
            DoctorName = consultation.Doctor?.FullName
        };
    }

    public static PrescriptionDto ToDto(Prescription prescription)
    {
        return new PrescriptionDto
        {
            Id = prescription.Id,
            ConsultationId = prescription.ConsultationId,
            PatientId = prescription.PatientId,
            DoctorId = prescription.DoctorId,
            MedicationName = prescription.MedicationName,
            Dosage = prescription.Dosage,
            Frequency = prescription.Frequency,
            Duration = prescription.Duration,
            Instructions = prescription.Instructions,
            PrescribedAtUtc = prescription.PrescribedAtUtc,
            PatientName = prescription.Patient?.FullName,
            DoctorName = prescription.Doctor?.FullName
        };
    }

    public static MedicalRecordDto ToDto(MedicalRecord medicalRecord)
    {
        return new MedicalRecordDto
        {
            Id = medicalRecord.Id,
            PatientId = medicalRecord.PatientId,
            DoctorId = medicalRecord.DoctorId,
            RecordType = medicalRecord.RecordType,
            Description = medicalRecord.Description,
            ClinicalNotes = medicalRecord.ClinicalNotes,
            RecordedAtUtc = medicalRecord.RecordedAtUtc,
            PatientName = medicalRecord.Patient?.FullName,
            DoctorName = medicalRecord.Doctor?.FullName
        };
    }

    public static User ToEntity(UserDto dto)
    {
        return new User
        {
            Id = dto.Id,
            FullName = dto.FullName,
            Email = dto.Email,
            Role = dto.Role,
            PhoneNumber = dto.PhoneNumber,
            IsActive = dto.IsActive
        };
    }

    public static Patient ToEntity(PatientDto dto)
    {
        return new Patient
        {
            Id = dto.Id,
            FullName = dto.FullName,
            PatientNumber = dto.PatientNumber,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            IdNumber = dto.IdNumber,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Address = dto.Address,
            IsActive = dto.IsActive
        };
    }

    public static Doctor ToEntity(DoctorDto dto)
    {
        return new Doctor
        {
            Id = dto.Id,
            FullName = dto.FullName,
            EmployeeNumber = dto.EmployeeNumber,
            Specialisation = dto.Specialisation,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Qualifications = dto.Qualifications,
            IsActive = dto.IsActive
        };
    }

    public static Appointment ToEntity(AppointmentDto dto)
    {
        return new Appointment
        {
            Id = dto.Id,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            AppointmentDateTime = dto.AppointmentDateTime,
            Status = dto.Status,
            Reason = dto.Reason,
            Notes = dto.Notes
        };
    }

    public static Consultation ToEntity(ConsultationDto dto)
    {
        return new Consultation
        {
            Id = dto.Id,
            AppointmentId = dto.AppointmentId,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            Symptoms = dto.Symptoms,
            Diagnosis = dto.Diagnosis,
            TreatmentPlan = dto.TreatmentPlan,
            ClinicalNotes = dto.ClinicalNotes
        };
    }

    public static Prescription ToEntity(PrescriptionDto dto)
    {
        return new Prescription
        {
            Id = dto.Id,
            ConsultationId = dto.ConsultationId,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            MedicationName = dto.MedicationName,
            Dosage = dto.Dosage,
            Frequency = dto.Frequency,
            Duration = dto.Duration,
            Instructions = dto.Instructions
        };
    }

    public static MedicalRecord ToEntity(MedicalRecordDto dto)
    {
        return new MedicalRecord
        {
            Id = dto.Id,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            RecordType = dto.RecordType,
            Description = dto.Description,
            ClinicalNotes = dto.ClinicalNotes
        };
    }
}