namespace SmartClinicManagementSystem.DTOs;

public class PatientSearchDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PatientNumber { get; set; } = string.Empty;
}