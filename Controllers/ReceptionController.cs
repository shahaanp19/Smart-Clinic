using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.DTOs;
using SmartClinicManagementSystem.Services.Interfaces;
using SmartClinicManagementSystem.Services.Mappers;

namespace SmartClinicManagementSystem.Controllers;

[Authorize(Policy = "ReceptionistOnly")]
public class ReceptionController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentService _appointmentService;

    public ReceptionController(
        IPatientService patientService,
        IDoctorService doctorService,
        IAppointmentService appointmentService)
    {
        _patientService = patientService;
        _doctorService = doctorService;
        _appointmentService = appointmentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var patients = await _patientService.GetAllAsync();
        var appointments = await _appointmentService.GetAllAsync();

        ViewBag.TotalPatients = patients.Count(p => p.IsActive);

        ViewBag.TotalAppointments = appointments.Count;

        ViewBag.TodayAppointments = appointments.Count(a =>
            a.AppointmentDateTime.ToLocalTime().Date == DateTime.Now.Date &&
            !string.Equals(
                a.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase));

        ViewBag.PendingAppointments = appointments.Count(a =>
            string.Equals(
                a.Status,
                "Scheduled",
                StringComparison.OrdinalIgnoreCase));

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Patients()
    {
        var patients = await _patientService.GetAllAsync();

        return View(
            patients
                .Where(p => p.IsActive)
                .Select(DtoMapper.ToDto)
                .ToList());
    }

    [HttpGet]
    public IActionResult RegisterPatient()
    {
        return View(new PatientDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPatient(
        PatientDto dto)
    {
        ValidatePatient(dto);

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var existingPatientNumber =
            await _patientService.GetByPatientNumberAsync(
                dto.PatientNumber);

        if (existingPatientNumber is not null)
        {
            ModelState.AddModelError(
                nameof(dto.PatientNumber),
                "A patient with this patient number already exists.");

            return View(dto);
        }

        var existingIdNumber =
            await _patientService.GetByIdNumberAsync(
                dto.IdNumber);

        if (existingIdNumber is not null)
        {
            ModelState.AddModelError(
                nameof(dto.IdNumber),
                "A patient with this ID number already exists.");

            return View(dto);
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var existingEmail =
                await _patientService.GetByEmailAsync(
                    dto.Email);

            if (existingEmail is not null)
            {
                ModelState.AddModelError(
                    nameof(dto.Email),
                    "A patient with this email address already exists.");

                return View(dto);
            }
        }

        try
        {
            var patient = DtoMapper.ToEntity(dto);

            await _patientService.CreateAsync(patient);

            TempData["SuccessMessage"] =
                $"Patient {patient.PatientNumber} has been registered successfully.";

            return RedirectToAction(nameof(Patients));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Appointments()
    {
        var fromUtc = DateTime.UtcNow.AddMonths(-1);
        var toUtc = DateTime.UtcNow.AddMonths(3);

        var appointments =
            await _appointmentService.GetAllAsync();

        var filteredAppointments = appointments
            .Where(a =>
                a.AppointmentDateTime >= fromUtc &&
                a.AppointmentDateTime <= toUtc)
            .OrderBy(a => a.AppointmentDateTime)
            .Select(DtoMapper.ToDto)
            .ToList();

        return View(filteredAppointments);
    }

    [HttpGet]
    public async Task<IActionResult> BookAppointment()
    {
        await PopulateAppointmentDataAsync();

        return View(new AppointmentDto
        {
            Status = "Scheduled"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookAppointment(
        AppointmentDto dto)
    {
        ValidateAppointment(dto);

        if (!ModelState.IsValid)
        {
            await PopulateAppointmentDataAsync();
            return View(dto);
        }

        var patient =
            await _patientService.GetByIdAsync(dto.PatientId);

        if (patient is null || !patient.IsActive)
        {
            ModelState.AddModelError(
                nameof(dto.PatientId),
                "The selected patient does not exist or is inactive.");

            await PopulateAppointmentDataAsync();
            return View(dto);
        }

        var doctor =
            await _doctorService.GetByIdAsync(dto.DoctorId);

        if (doctor is null || !doctor.IsActive)
        {
            ModelState.AddModelError(
                nameof(dto.DoctorId),
                "The selected doctor does not exist or is inactive.");

            await PopulateAppointmentDataAsync();
            return View(dto);
        }

        try
        {
            var appointment = DtoMapper.ToEntity(dto);

            appointment.PatientId = patient.Id;
            appointment.DoctorId = doctor.Id;
            appointment.Status = "Scheduled";

            await _appointmentService.CreateAsync(appointment);

            TempData["SuccessMessage"] =
                "Appointment has been booked successfully.";

            return RedirectToAction(nameof(Appointments));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                nameof(dto.AppointmentDateTime),
                ex.Message);

            await PopulateAppointmentDataAsync();
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] =
                "The selected appointment is invalid.";

            return RedirectToAction(nameof(Appointments));
        }

        var appointment =
            await _appointmentService.GetByIdAsync(id);

        if (appointment is null)
        {
            TempData["ErrorMessage"] =
                "The selected appointment could not be found.";

            return RedirectToAction(nameof(Appointments));
        }

        if (string.Equals(
            appointment.Status,
            "Cancelled",
            StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] =
                "This appointment has already been cancelled.";

            return RedirectToAction(nameof(Appointments));
        }

        try
        {
            var cancelled =
                await _appointmentService.CancelAsync(id);

            TempData[cancelled
                ? "SuccessMessage"
                : "ErrorMessage"] =
                cancelled
                    ? "Appointment cancelled successfully."
                    : "The appointment could not be cancelled.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Appointments));
    }

    [HttpGet]
    [Produces("application/json")]
    public async Task<IActionResult> SearchPatients(
        string? search)
    {
        if (search?.Length > 100)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid search request",
                Detail = "The search term cannot exceed 100 characters.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var patients =
            await _patientService.SearchAsync(search);

        var results = patients
            .Select(DtoMapper.ToSearchDto)
            .ToList();

        return Ok(results);
    }

    private async Task PopulateAppointmentDataAsync()
    {
        var patients =
            await _patientService.GetAllAsync();

        var doctors =
            await _doctorService.GetAllAsync();

        ViewBag.Patients = patients
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Doctors = doctors
            .Where(d => d.IsActive)
            .OrderBy(d => d.FullName)
            .Select(DtoMapper.ToDto)
            .ToList();
    }

    private void ValidatePatient(
        PatientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            ModelState.AddModelError(
                nameof(dto.FullName),
                "Patient name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.PatientNumber))
        {
            ModelState.AddModelError(
                nameof(dto.PatientNumber),
                "Patient number is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.IdNumber))
        {
            ModelState.AddModelError(
                nameof(dto.IdNumber),
                "ID number is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            ModelState.AddModelError(
                nameof(dto.PhoneNumber),
                "Phone number is required.");
        }

        if (dto.DateOfBirth == default)
        {
            ModelState.AddModelError(
                nameof(dto.DateOfBirth),
                "Date of birth is required.");
        }
        else if (dto.DateOfBirth.Date > DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(dto.DateOfBirth),
                "Date of birth cannot be in the future.");
        }
    }

    private void ValidateAppointment(
        AppointmentDto dto)
    {
        if (dto.PatientId <= 0)
        {
            ModelState.AddModelError(
                nameof(dto.PatientId),
                "Please select a patient.");
        }

        if (dto.DoctorId <= 0)
        {
            ModelState.AddModelError(
                nameof(dto.DoctorId),
                "Please select a doctor.");
        }

        if (dto.AppointmentDateTime == default)
        {
            ModelState.AddModelError(
                nameof(dto.AppointmentDateTime),
                "Please select an appointment date and time.");
        }
        else if (dto.AppointmentDateTime <= DateTime.UtcNow)
        {
            ModelState.AddModelError(
                nameof(dto.AppointmentDateTime),
                "Appointment date and time must be in the future.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            ModelState.AddModelError(
                nameof(dto.Reason),
                "Please provide a reason for the appointment.");
        }
    }
}