using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.DTOs;
using SmartClinicManagementSystem.Services.Interfaces;
using SmartClinicManagementSystem.Services.Mappers;

namespace SmartClinicManagementSystem.Controllers;

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
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(DtoMapper.ToDto(patient));
    }

    [HttpGet]
    public async Task<IActionResult> BookAppointment()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddMonths(3);

        var appointments = await _appointmentService.GetByPatientAsync(
            patient.Id,
            fromUtc,
            toUtc);

        var doctors = await _doctorService.GetAllAsync();

        ViewBag.Appointments = appointments
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Doctors = doctors
            .Where(d => d.IsActive)
            .Select(DtoMapper.ToDto)
            .ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookAppointment(AppointmentDto dto)
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction(nameof(Login));
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
            return RedirectToAction(nameof(Login));
        }

        var appointment = await _appointmentService.GetByIdAsync(id);

        if (appointment is null || appointment.PatientId != patient.Id)
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

        var cancelled = await _appointmentService.CancelAsync(id);

        TempData[cancelled ? "SuccessMessage" : "ErrorMessage"] =
            cancelled
                ? "Your appointment has been cancelled successfully."
                : "The appointment could not be cancelled.";

        return RedirectToAction(nameof(BookAppointment));
    }

    [HttpGet]
    public async Task<IActionResult> MedicalHistory()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> PatientIntakeForm()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(DtoMapper.ToDto(patient));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(DtoMapper.ToDto(patient));
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction(nameof(Login));
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

        return await _patientService.GetByEmailAsync(email);
    }

    private async Task PopulateAppointmentViewDataAsync(int patientId)
    {
        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddMonths(3);

        var appointments = await _appointmentService.GetByPatientAsync(
            patientId,
            fromUtc,
            toUtc);

        var doctors = await _doctorService.GetAllAsync();

        ViewBag.Appointments = appointments
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Doctors = doctors
            .Where(d => d.IsActive)
            .Select(DtoMapper.ToDto)
            .ToList();
    }
}