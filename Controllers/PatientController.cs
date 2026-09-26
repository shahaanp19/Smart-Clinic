using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.DTOs;
using SmartClinicManagementSystem.Services.Interfaces;
using SmartClinicManagementSystem.Services.Mappers;

namespace SmartClinicManagementSystem.Controllers;

[Authorize(Policy = "PatientOnly")]
public class PatientController : Controller
{
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;
    private readonly IDoctorService _doctorService;

    public PatientController(
        IPatientService patientService,
        IAppointmentService appointmentService,
        IDoctorService doctorService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
        _doctorService = doctorService;
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
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            await SignOutInvalidPatientSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        return View(DtoMapper.ToDto(patient));
    }

    [HttpGet]
    public async Task<IActionResult> BookAppointment()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            await SignOutInvalidPatientSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        await PopulateAppointmentViewDataAsync(patient.Id);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookAppointment(AppointmentDto dto)
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            await SignOutInvalidPatientSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        if (dto.DoctorId <= 0)
        {
            ModelState.AddModelError(
                nameof(dto.DoctorId),
                "Please select a doctor.");
        }

        if (dto.AppointmentDateTime <= DateTime.UtcNow)
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

        if (!ModelState.IsValid)
        {
            await PopulateAppointmentViewDataAsync(patient.Id);
            return View(dto);
        }

        var doctor = await _doctorService.GetByIdAsync(dto.DoctorId);

        if (doctor is null || !doctor.IsActive)
        {
            ModelState.AddModelError(
                nameof(dto.DoctorId),
                "The selected doctor is not available.");

            await PopulateAppointmentViewDataAsync(patient.Id);
            return View(dto);
        }

        var appointment = DtoMapper.ToEntity(dto);

        appointment.PatientId = patient.Id;
        appointment.Status = "Scheduled";

        try
        {
            await _appointmentService.CreateAsync(appointment);

            TempData["SuccessMessage"] =
                "Your appointment has been successfully booked.";

            return RedirectToAction(nameof(BookAppointment));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                nameof(dto.AppointmentDateTime),
                ex.Message);

            await PopulateAppointmentViewDataAsync(patient.Id);

            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            await SignOutInvalidPatientSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        var appointment = await _appointmentService.GetByIdAsync(id);

        if (appointment is null ||
            appointment.PatientId != patient.Id)
        {
            TempData["ErrorMessage"] =
                "The selected appointment could not be found.";

            return RedirectToAction(nameof(BookAppointment));
        }

        if (appointment.Status.Equals(
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] =
                "This appointment has already been cancelled.";

            return RedirectToAction(nameof(BookAppointment));
        }

        var cancelled =
            await _appointmentService.CancelAsync(id);

        TempData[cancelled ? "SuccessMessage" : "ErrorMessage"] =
            cancelled
                ? "Your appointment has been cancelled successfully."
                : "The appointment could not be cancelled.";

        return RedirectToAction(nameof(BookAppointment));
    }

    [HttpGet]
    public async Task<IActionResult> MedicalHistory()
    {
        if (!await EnsureCurrentPatientAsync())
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        if (!await EnsureCurrentPatientAsync())
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> PatientIntakeForm()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            await SignOutInvalidPatientSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        return View(DtoMapper.ToDto(patient));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            await SignOutInvalidPatientSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        return View(DtoMapper.ToDto(patient));
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            await SignOutInvalidPatientSessionAsync();
            return RedirectToAction("Login", "Account");
        }

        return View(DtoMapper.ToDto(patient));
    }

    private async Task<Models.Patient?> GetCurrentPatientAsync()
    {
        var email = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var patient = await _patientService.GetByEmailAsync(email);

        if (patient is null || !patient.IsActive)
        {
            return null;
        }

        return patient;
    }

    private async Task<bool> EnsureCurrentPatientAsync()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is not null)
        {
            return true;
        }

        await SignOutInvalidPatientSessionAsync();
        return false;
    }

    private async Task PopulateAppointmentViewDataAsync(int patientId)
    {
        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddMonths(3);

        var appointments =
            await _appointmentService.GetByPatientAsync(
                patientId,
                fromUtc,
                toUtc);

        var doctors =
            await _doctorService.GetAllAsync();

        ViewBag.Appointments = appointments
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Doctors = doctors
            .Where(d => d.IsActive)
            .Select(DtoMapper.ToDto)
            .ToList();
    }

    private async Task SignOutInvalidPatientSessionAsync()
    {
        await HttpContext.SignOutAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults
                .AuthenticationScheme);
    }
}