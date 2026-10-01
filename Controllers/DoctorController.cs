using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.DTOs;
using SmartClinicManagementSystem.Services.Interfaces;
using SmartClinicManagementSystem.Services.Mappers;

namespace SmartClinicManagementSystem.Controllers;

[Authorize(Policy = "DoctorOnly")]
public class DoctorController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;
    private readonly IConsultationService _consultationService;
    private readonly IPrescriptionService _prescriptionService;

    public DoctorController(
        IDoctorService doctorService,
        IPatientService patientService,
        IAppointmentService appointmentService,
        IConsultationService consultationService,
        IPrescriptionService prescriptionService)
    {
        _doctorService = doctorService;
        _patientService = patientService;
        _appointmentService = appointmentService;
        _consultationService = consultationService;
        _prescriptionService = prescriptionService;
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
        var doctor = await GetCurrentDoctorAsync();

        if (doctor is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddMonths(3);

        var appointments =
            await _appointmentService.GetByDoctorAsync(
                doctor.Id,
                fromUtc,
                toUtc);

        var consultations =
            await _consultationService.GetByDoctorAsync(
                doctor.Id);

        ViewBag.Doctor = DtoMapper.ToDto(doctor);
        ViewBag.TotalConsultations = consultations.Count;

        ViewBag.CompletedConsultations =
            consultations.Count;

        ViewBag.PendingConsultations =
            appointments.Count(a =>
                string.Equals(
                    a.Status,
                    "Scheduled",
                    StringComparison.OrdinalIgnoreCase));

        ViewBag.TodayAppointments =
            appointments.Count(a =>
                a.AppointmentDateTime.ToLocalTime().Date ==
                DateTime.Now.Date &&
                !string.Equals(
                    a.Status,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase));

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var appointments =
            await _appointmentService.GetByDoctorAsync(
                doctor.Id,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMonths(3));

        ViewBag.Appointments = appointments
            .Select(DtoMapper.ToDto)
            .ToList();

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Consultation()
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor is null)
        {
            return RedirectToAction("Login", "Account");
        }

        await PopulateConsultationDataAsync(doctor.Id);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consultation(
        int patientId,
        int appointmentId,
        string symptoms,
        string diagnosis,
        string treatmentPlan,
        string clinicalNotes)
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (patientId <= 0)
        {
            ModelState.AddModelError(
                nameof(patientId),
                "A valid patient is required.");
        }

        if (appointmentId <= 0)
        {
            ModelState.AddModelError(
                nameof(appointmentId),
                "A valid appointment is required.");
        }

        if (string.IsNullOrWhiteSpace(symptoms))
        {
            ModelState.AddModelError(
                nameof(symptoms),
                "Symptoms are required.");
        }

        if (string.IsNullOrWhiteSpace(diagnosis))
        {
            ModelState.AddModelError(
                nameof(diagnosis),
                "Diagnosis is required.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateConsultationDataAsync(doctor.Id);
            return View();
        }

        var appointment =
            await _appointmentService.GetByIdAsync(
                appointmentId);

        if (appointment is null)
        {
            ModelState.AddModelError(
                nameof(appointmentId),
                "The selected appointment could not be found.");

            await PopulateConsultationDataAsync(doctor.Id);
            return View();
        }

        if (appointment.DoctorId != doctor.Id)
        {
            ModelState.AddModelError(
                nameof(appointmentId),
                "You are not authorised to use this appointment.");

            await PopulateConsultationDataAsync(doctor.Id);
            return View();
        }

        if (appointment.PatientId != patientId)
        {
            ModelState.AddModelError(
                nameof(patientId),
                "The selected patient does not match the appointment.");

            await PopulateConsultationDataAsync(doctor.Id);
            return View();
        }

        if (string.Equals(
            appointment.Status,
            "Cancelled",
            StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(appointmentId),
                "Cancelled appointments cannot be used for consultations.");

            await PopulateConsultationDataAsync(doctor.Id);
            return View();
        }

        var existingConsultation =
            await _consultationService.GetByAppointmentIdAsync(
                appointmentId);

        if (existingConsultation is not null)
        {
            ModelState.AddModelError(
                nameof(appointmentId),
                "A consultation has already been recorded for this appointment.");

            await PopulateConsultationDataAsync(doctor.Id);
            return View();
        }

        var consultation = new Models.Consultation
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            DoctorId = doctor.Id,
            Symptoms = symptoms.Trim(),
            Diagnosis = diagnosis.Trim(),
            TreatmentPlan =
                string.IsNullOrWhiteSpace(treatmentPlan)
                    ? null
                    : treatmentPlan.Trim(),
            ClinicalNotes =
                string.IsNullOrWhiteSpace(clinicalNotes)
                    ? null
                    : clinicalNotes.Trim(),
            ConsultationDateUtc = DateTime.UtcNow
        };

        try
        {
            await _consultationService.CreateAsync(
                consultation);

            TempData["SuccessMessage"] =
                "Consultation has been recorded successfully.";

            return RedirectToAction(nameof(Consultation));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await PopulateConsultationDataAsync(
                doctor.Id);

            return View();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await PopulateConsultationDataAsync(
                doctor.Id);

            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GeneratePrescription()
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor is null)
        {
            return RedirectToAction("Login", "Account");
        }

        await PopulatePrescriptionDataAsync(
            doctor.Id);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePrescription(
        int patientId,
        int consultationId,
        string medicationName,
        string dosage,
        string frequency,
        string duration,
        string instructions)
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (patientId <= 0)
        {
            ModelState.AddModelError(
                nameof(patientId),
                "A valid patient is required.");
        }

        if (consultationId <= 0)
        {
            ModelState.AddModelError(
                nameof(consultationId),
                "A valid consultation is required.");
        }

        if (string.IsNullOrWhiteSpace(medicationName))
        {
            ModelState.AddModelError(
                nameof(medicationName),
                "Medication is required.");
        }

        if (string.IsNullOrWhiteSpace(dosage))
        {
            ModelState.AddModelError(
                nameof(dosage),
                "Dosage is required.");
        }

        if (string.IsNullOrWhiteSpace(frequency))
        {
            ModelState.AddModelError(
                nameof(frequency),
                "Frequency is required.");
        }

        if (string.IsNullOrWhiteSpace(duration))
        {
            ModelState.AddModelError(
                nameof(duration),
                "Duration is required.");
        }

        if (!ModelState.IsValid)
        {
            await PopulatePrescriptionDataAsync(
                doctor.Id);

            return View();
        }

        var consultation =
            await _consultationService.GetByIdAsync(
                consultationId);

        if (consultation is null)
        {
            ModelState.AddModelError(
                nameof(consultationId),
                "The selected consultation could not be found.");

            await PopulatePrescriptionDataAsync(
                doctor.Id);

            return View();
        }

        if (consultation.DoctorId != doctor.Id)
        {
            ModelState.AddModelError(
                nameof(consultationId),
                "You are not authorised to use this consultation.");

            await PopulatePrescriptionDataAsync(
                doctor.Id);

            return View();
        }

        if (consultation.PatientId != patientId)
        {
            ModelState.AddModelError(
                nameof(patientId),
                "The selected patient does not match the consultation.");

            await PopulatePrescriptionDataAsync(
                doctor.Id);

            return View();
        }

        var prescription = new Models.Prescription
        {
            ConsultationId = consultationId,
            PatientId = patientId,
            DoctorId = doctor.Id,
            MedicationName = medicationName.Trim(),
            Dosage = dosage.Trim(),
            Frequency = frequency.Trim(),
            Duration = duration.Trim(),
            Instructions =
                string.IsNullOrWhiteSpace(instructions)
                    ? null
                    : instructions.Trim(),
            PrescribedAtUtc = DateTime.UtcNow
        };

        try
        {
            await _prescriptionService.CreateAsync(
                prescription);

            TempData["SuccessMessage"] =
                "Prescription has been generated successfully.";

            return RedirectToAction(
                nameof(GeneratePrescription));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await PopulatePrescriptionDataAsync(
                doctor.Id);

            return View();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await PopulatePrescriptionDataAsync(
                doctor.Id);

            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> MedicalRecords()
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var consultations =
            await _consultationService.GetByDoctorAsync(
                doctor.Id);

        var prescriptions =
            await _prescriptionService.GetByDoctorAsync(
                doctor.Id);

        ViewBag.Consultations = consultations
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Prescriptions = prescriptions
            .Select(DtoMapper.ToDto)
            .ToList();

        return View();
    }

    private async Task<Models.Doctor?> GetCurrentDoctorAsync()
    {
        var email = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var doctor =
            await _doctorService.GetByEmailAsync(email);

        if (doctor is null || !doctor.IsActive)
        {
            return null;
        }

        return doctor;
    }

    private async Task PopulateConsultationDataAsync(
        int doctorId)
    {
        var appointments =
            await _appointmentService.GetByDoctorAsync(
                doctorId,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddMonths(3));

        var consultations =
            await _consultationService.GetByDoctorAsync(
                doctorId);

        var patients =
            await _patientService.GetAllAsync();

        var completedAppointmentIds =
            consultations
                .Select(c => c.AppointmentId)
                .ToHashSet();

        ViewBag.Patients = patients
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Appointments = appointments
            .Where(a =>
                !string.Equals(
                    a.Status,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase) &&
                !completedAppointmentIds.Contains(a.Id))
            .OrderBy(a => a.AppointmentDateTime)
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Consultations = consultations
            .Select(DtoMapper.ToDto)
            .ToList();
    }

    private async Task PopulatePrescriptionDataAsync(
        int doctorId)
    {
        var consultations =
            await _consultationService.GetByDoctorAsync(
                doctorId);

        var patients =
            await _patientService.GetAllAsync();

        var prescriptions =
            await _prescriptionService.GetByDoctorAsync(
                doctorId);

        ViewBag.Patients = patients
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Consultations = consultations
            .OrderByDescending(
                c => c.ConsultationDateUtc)
            .Select(DtoMapper.ToDto)
            .ToList();

        ViewBag.Prescriptions = prescriptions
            .Select(DtoMapper.ToDto)
            .ToList();
    }
}