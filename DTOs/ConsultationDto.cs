using System.ComponentModel.DataAnnotations;

namespace SmartClinicManagementSystem.DTOs;

public class ConsultationDto
{
    public int Id { get; set; }

    [Required]
    public int AppointmentId { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Symptoms { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? TreatmentPlan { get; set; }

    [StringLength(2000)]
    public string? ClinicalNotes { get; set; }

    public DateTime ConsultationDateUtc { get; set; }

    public string? PatientName { get; set; }

    public string? DoctorName { get; set; }
}