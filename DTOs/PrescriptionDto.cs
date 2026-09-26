using System.ComponentModel.DataAnnotations;

namespace SmartClinicManagementSystem.DTOs;

public class PrescriptionDto
{
    public int Id { get; set; }

    [Required]
    public int ConsultationId { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    [StringLength(200)]
    public string MedicationName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Dosage { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Frequency { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Duration { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Instructions { get; set; }

    public DateTime PrescribedAtUtc { get; set; }

    public string? PatientName { get; set; }

    public string? DoctorName { get; set; }
}