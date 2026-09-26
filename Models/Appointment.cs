using System.ComponentModel.DataAnnotations;

namespace SmartClinicManagementSystem.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    public DateTime AppointmentDateTime { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Scheduled";

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public Patient? Patient { get; set; }

    public Doctor? Doctor { get; set; }
}