using System.ComponentModel.DataAnnotations;

namespace SmartClinicManagementSystem.DTOs;

public class AppointmentDto
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

    public string? PatientName { get; set; }

    public string? DoctorName { get; set; }
}