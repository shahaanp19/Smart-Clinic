using System.ComponentModel.DataAnnotations;

namespace SmartClinicManagementSystem.Models;

public class Prescription
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

    public DateTime PrescribedAtUtc { get; set; } = DateTime.UtcNow;

    public Consultation? Consultation { get; set; }

    public Patient? Patient { get; set; }

    public Doctor? Doctor { get; set; }
}