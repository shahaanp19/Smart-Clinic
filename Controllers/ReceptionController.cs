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

        ViewBag.TotalPatients = patients.Count;
        ViewBag.TotalAppointments = appointments.Count;

        ViewBag.TodayAppointments = appointments.Count(a =>
            a.AppointmentDateTime.ToLocalTime().Date ==
            DateTime.Now.Date &&
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

        ViewBag.Patients = patients
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(DtoMapper.ToDto)
            .ToList();

        return View();
    }

    [HttpGet]
    public IActionResult RegisterPatient()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPatient(PatientDto dto)
    {
        ValidatePatient(dto);

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var existingByPatientNumber =
            await _patientService.GetByPatientNumberAsync(
                dto.PatientNumber);

        if (existingByPatientNumber is not null)
        {
            ModelState.AddModelError(
                nameof(dto.PatientNumber),
                "A patient with this patient number already exists.");

            return View(dto);
        }

        if (!string.IsNullOrWhiteSpace(dto.IdNumber))
        {
            var existingByIdNumber =
                await _patientService.GetByIdNumberAsync(
                    dto.IdNumber);

            if (existingByIdNumber is not null)
            {
                ModelState.AddModelError(
                    nameof(dto.IdNumber),
                    "A patient with this ID number already exists.");

                return View(dto);
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var existingByEmail =
                await _patientService.GetByEmailAsync(
                    dto.Email);

            if (existingByEmail is not null)
            {
                ModelState.AddModelError(
                    nameof(dto.Email),
                    "A patient with this email address already exists.");

                return View(dto);
            }
        }

        var patient = DtoMapper.ToEntity(dto);

        try
        {
            await _patientService.CreateAsync(patient);

            TempData["SuccessMessage"] =
                "Patient registered successfully.";

            return RedirectToAction(nameof(Patients));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(dto);
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

        ViewBag.Appointments = appointments
            .Where(a =>
                a.AppointmentDateTime >= fromUtc &&
                a.AppointmentDateTime <= toUtc)
            .OrderBy(a => a.AppointmentDateTime)
            .Select(DtoMapper.ToDto)
            .ToList();

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> BookAppointment()
    {
        await PopulateAppointmentViewDataAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookAppointment(
        AppointmentDto dto)
    {
        ValidateAppointment(dto);

        if (!ModelState.IsValid)
        {
            await PopulateAppointmentViewDataAsync();
            return View(dto);
        }

        var patient =
            await _patientService.GetByIdAsync(dto.PatientId);

        if (patient is null || !patient.IsActive)
        {
            ModelState.AddModelError(
                nameof(dto.PatientId),
                "The selected patient is not available.");

            await PopulateAppointmentViewDataAsync();
            return View(dto);
        }

        var doctor =
            await _doctorService.GetByIdAsync(dto.DoctorId);

        if (doctor is null || !doctor.IsActive)
        {
            ModelState.AddModelError(
                nameof(dto.DoctorId),
                "The selected doctor is not available.");

            await PopulateAppointmentViewDataAsync();
            return View(dto);
        }

        var appointment = DtoMapper.ToEntity(dto);

        appointment.PatientId = patient.Id;
        appointment.DoctorId = doctor.Id;
        appointment.Status = "Scheduled";

        try
        {
            await _appointmentService.CreateAsync(appointment);

            TempData["SuccessMessage"] =
                "Appointment booked successfully.";

            return RedirectToAction(nameof(Appointments));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await PopulateAppointmentViewDataAsync();
            return View(dto);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await PopulateAppointmentViewDataAsync();
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
                "The selected appointment could not be found.";

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
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
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
        search = search?.Trim();

        if (search is not null && search.Length > 100)
        {
            return BadRequest(new
            {
                message = "Search text cannot exceed 100 characters."
            });
        }

        var patients =
            await _patientService.SearchAsync(search);

        var results = patients
            .Where(p => p.IsActive)
            .Select(DtoMapper.ToSearchDto)
            .ToList();

        return Ok(results);
    }

    private async Task PopulateAppointmentViewDataAsync()
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

    private void ValidatePatient(PatientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            ModelState.AddModelError(
                nameof(dto.FullName),
                "Patient full name is required.");
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

        if (dto.DateOfBirth > DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(
                nameof(dto.DateOfBirth),
                "Date of birth cannot be in the future.");
        }
    }

    private void ValidateAppointment(AppointmentDto dto)
    {
        if (dto.PatientId <= 0)
        {
            ModelState.AddModelError(
                nameof(dto.PatientId),
                "Please select a valid patient.");
        }

        if (dto.DoctorId <= 0)
        {
            ModelState.AddModelError(
                nameof(dto.DoctorId),
                "Please select a valid doctor.");
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
                "Appointment reason is required.");
        }
    }
}