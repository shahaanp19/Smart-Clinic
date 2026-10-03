using System.ComponentModel.DataAnnotations;

namespace SmartClinicManagementSystem.Models;

public class Consultation
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

    public DateTime ConsultationDateUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public Appointment? Appointment { get; set; }

    public Patient? Patient { get; set; }

    public Doctor? Doctor { get; set; }

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}