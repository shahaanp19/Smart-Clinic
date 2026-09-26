using System.ComponentModel.DataAnnotations;

namespace SmartClinicManagementSystem.DTOs;

public class MedicalRecordDto
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    [StringLength(200)]
    public string RecordType { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? ClinicalNotes { get; set; }

    public DateTime RecordedAtUtc { get; set; }

    public string? PatientName { get; set; }

    public string? DoctorName { get; set; }
}