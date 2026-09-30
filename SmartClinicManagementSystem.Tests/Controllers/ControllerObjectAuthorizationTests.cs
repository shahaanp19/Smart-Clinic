using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using SmartClinicManagementSystem.Controllers;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Services.Interfaces;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class ControllerObjectAuthorizationTests
    : IClassFixture<ControllerTestFactory>
{
    private readonly ControllerTestFactory _factory;

    public ControllerObjectAuthorizationTests(
        ControllerTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Patient_CannotCancelAnotherPatientsAppointment()
    {
        using var scope = _factory.Services.CreateScope();

        var patientService =
            scope.ServiceProvider
                .GetRequiredService<IPatientService>();

        var appointmentService =
            scope.ServiceProvider
                .GetRequiredService<IAppointmentService>();

        var doctorService =
            scope.ServiceProvider
                .GetRequiredService<IDoctorService>();

        var patientA = new Patient
        {
            FullName = "Authorization Patient A",
            Email = $"auth.patient.a.{Guid.NewGuid():N}@test.local",
            PatientNumber = $"AUTH-A-{Guid.NewGuid():N}",
            IdNumber = $"AUTH-A-{Guid.NewGuid():N}",
            PhoneNumber = "0110000001",
            DateOfBirth = new DateTime(1990, 1, 1),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var patientB = new Patient
        {
            FullName = "Authorization Patient B",
            Email = $"auth.patient.b.{Guid.NewGuid():N}@test.local",
            PatientNumber = $"AUTH-B-{Guid.NewGuid():N}",
            IdNumber = $"AUTH-B-{Guid.NewGuid():N}",
            PhoneNumber = "0110000002",
            DateOfBirth = new DateTime(1991, 1, 1),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctor = new Doctor
        {
            FullName = "Authorization Doctor",
            Email = $"auth.doctor.{Guid.NewGuid():N}@test.local",
            EmployeeNumber = $"AUTH-D-{Guid.NewGuid():N}",
            Specialisation = "General Practice",
            PhoneNumber = "0110000011",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await patientService.CreateAsync(patientA);
        await patientService.CreateAsync(patientB);
        await doctorService.CreateAsync(doctor);

        var appointment = new Appointment
        {
            PatientId = patientB.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(2),
            Reason = "Authorization test appointment",
            Status = "Scheduled",
            CreatedAtUtc = DateTime.UtcNow
        };

        await appointmentService.CreateAsync(appointment);

        var controller = new PatientController(
            patientService,
            appointmentService,
            doctorService);

        SetAuthenticatedUser(
            controller,
            patientA.Email!,
            "Patient");

        var result =
            await controller.CancelAppointment(
                appointment.Id);

        var redirect =
            Assert.IsType<RedirectToActionResult>(
                result);

        Assert.Equal(
            nameof(PatientController.BookAppointment),
            redirect.ActionName);

        Assert.Equal(
            "The selected appointment could not be found.",
            controller.TempData["ErrorMessage"]);

        var persistedAppointment =
            await appointmentService.GetByIdAsync(
                appointment.Id);

        Assert.NotNull(persistedAppointment);

        Assert.Equal(
            "Scheduled",
            persistedAppointment!.Status);

        Assert.Equal(
            patientB.Id,
            persistedAppointment.PatientId);
    }

    [Fact]
    public async Task Doctor_CannotCreateConsultationForAnotherDoctorsAppointment()
    {
        using var scope = _factory.Services.CreateScope();

        var patientService =
            scope.ServiceProvider
                .GetRequiredService<IPatientService>();

        var doctorService =
            scope.ServiceProvider
                .GetRequiredService<IDoctorService>();

        var appointmentService =
            scope.ServiceProvider
                .GetRequiredService<IAppointmentService>();

        var consultationService =
            scope.ServiceProvider
                .GetRequiredService<IConsultationService>();

        var prescriptionService =
            scope.ServiceProvider
                .GetRequiredService<IPrescriptionService>();

        var patient = new Patient
        {
            FullName = "Consultation Authorization Patient",
            Email = $"auth.consultation.patient.{Guid.NewGuid():N}@test.local",
            PatientNumber = $"AUTH-C-P-{Guid.NewGuid():N}",
            IdNumber = $"AUTH-C-P-{Guid.NewGuid():N}",
            PhoneNumber = "0110000010",
            DateOfBirth = new DateTime(1988, 1, 1),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctorA = new Doctor
        {
            FullName = "Consultation Doctor A",
            Email = $"auth.consultation.doctor.a.{Guid.NewGuid():N}@test.local",
            EmployeeNumber = $"AUTH-C-A-{Guid.NewGuid():N}",
            Specialisation = "General Practice",
            PhoneNumber = "0110000011",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctorB = new Doctor
        {
            FullName = "Consultation Doctor B",
            Email = $"auth.consultation.doctor.b.{Guid.NewGuid():N}@test.local",
            EmployeeNumber = $"AUTH-C-B-{Guid.NewGuid():N}",
            Specialisation = "Cardiology",
            PhoneNumber = "0110000012",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await patientService.CreateAsync(patient);
        await doctorService.CreateAsync(doctorA);
        await doctorService.CreateAsync(doctorB);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctorB.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(3),
            Reason = "Doctor ownership test",
            Status = "Scheduled",
            CreatedAtUtc = DateTime.UtcNow
        };

        await appointmentService.CreateAsync(appointment);

        var controller = new DoctorController(
            doctorService,
            patientService,
            appointmentService,
            consultationService,
            prescriptionService);

        SetAuthenticatedUser(
            controller,
            doctorA.Email!,
            "Doctor");

        var result =
            await controller.Consultation(
                patient.Id,
                appointment.Id,
                "Headache",
                "Test diagnosis",
                "Test treatment",
                "Test notes");

        Assert.IsType<ViewResult>(result);

        Assert.False(
            controller.ModelState.IsValid);

        Assert.Contains(
            controller.ModelState.Values,
            entry => entry.Errors.Any(error =>
                error.ErrorMessage.Contains(
                    "not authorised",
                    StringComparison.OrdinalIgnoreCase)));

        Assert.Null(
            await consultationService
                .GetByAppointmentIdAsync(
                    appointment.Id));
    }

    [Fact]
    public async Task Doctor_CannotCreatePrescriptionForAnotherDoctorsConsultation()
    {
        using var scope = _factory.Services.CreateScope();

        var patientService =
            scope.ServiceProvider
                .GetRequiredService<IPatientService>();

        var doctorService =
            scope.ServiceProvider
                .GetRequiredService<IDoctorService>();

        var appointmentService =
            scope.ServiceProvider
                .GetRequiredService<IAppointmentService>();

        var consultationService =
            scope.ServiceProvider
                .GetRequiredService<IConsultationService>();

        var prescriptionService =
            scope.ServiceProvider
                .GetRequiredService<IPrescriptionService>();

        var patient = new Patient
        {
            FullName = "Prescription Authorization Patient",
            Email = $"auth.prescription.patient.{Guid.NewGuid():N}@test.local",
            PatientNumber = $"AUTH-R-P-{Guid.NewGuid():N}",
            IdNumber = $"AUTH-R-P-{Guid.NewGuid():N}",
            PhoneNumber = "0110000020",
            DateOfBirth = new DateTime(1987, 1, 1),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctorA = new Doctor
        {
            FullName = "Prescription Doctor A",
            Email = $"auth.prescription.doctor.a.{Guid.NewGuid():N}@test.local",
            EmployeeNumber = $"AUTH-R-A-{Guid.NewGuid():N}",
            Specialisation = "General Practice",
            PhoneNumber = "0110000021",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctorB = new Doctor
        {
            FullName = "Prescription Doctor B",
            Email = $"auth.prescription.doctor.b.{Guid.NewGuid():N}@test.local",
            EmployeeNumber = $"AUTH-R-B-{Guid.NewGuid():N}",
            Specialisation = "Dermatology",
            PhoneNumber = "0110000022",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await patientService.CreateAsync(patient);
        await doctorService.CreateAsync(doctorA);
        await doctorService.CreateAsync(doctorB);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctorB.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(4),
            Reason = "Prescription ownership test",
            Status = "Scheduled",
            CreatedAtUtc = DateTime.UtcNow
        };

        await appointmentService.CreateAsync(appointment);

        var consultation = new Consultation
        {
            AppointmentId = appointment.Id,
            PatientId = patient.Id,
            DoctorId = doctorB.Id,
            Symptoms = "Test symptoms",
            Diagnosis = "Test diagnosis",
            TreatmentPlan = "Test treatment",
            ClinicalNotes = "Test notes",
            ConsultationDateUtc = DateTime.UtcNow
        };

        await consultationService.CreateAsync(
            consultation);

        var controller = new DoctorController(
            doctorService,
            patientService,
            appointmentService,
            consultationService,
            prescriptionService);

        SetAuthenticatedUser(
            controller,
            doctorA.Email!,
            "Doctor");

        var result =
            await controller.GeneratePrescription(
                patient.Id,
                consultation.Id,
                "Test Medication",
                "500mg",
                "Twice daily",
                "7 days",
                "Test instructions");

        Assert.IsType<ViewResult>(result);

        Assert.False(
            controller.ModelState.IsValid);

        Assert.Contains(
            controller.ModelState.Values,
            entry => entry.Errors.Any(error =>
                error.ErrorMessage.Contains(
                    "not authorised",
                    StringComparison.OrdinalIgnoreCase)));

        var prescriptions =
            await prescriptionService.GetByDoctorAsync(
                doctorA.Id);

        Assert.DoesNotContain(
            prescriptions,
            prescription =>
                prescription.ConsultationId ==
                consultation.Id);
    }

    [Fact]
    public async Task Doctor_CannotCreateConsultationWhenPatientDoesNotMatchAppointment()
    {
        using var scope = _factory.Services.CreateScope();

        var patientService =
            scope.ServiceProvider
                .GetRequiredService<IPatientService>();

        var doctorService =
            scope.ServiceProvider
                .GetRequiredService<IDoctorService>();

        var appointmentService =
            scope.ServiceProvider
                .GetRequiredService<IAppointmentService>();

        var consultationService =
            scope.ServiceProvider
                .GetRequiredService<IConsultationService>();

        var prescriptionService =
            scope.ServiceProvider
                .GetRequiredService<IPrescriptionService>();

        var patientA = new Patient
        {
            FullName = "Matching Patient A",
            Email = $"auth.match.a.{Guid.NewGuid():N}@test.local",
            PatientNumber = $"AUTH-M-A-{Guid.NewGuid():N}",
            IdNumber = $"AUTH-M-A-{Guid.NewGuid():N}",
            PhoneNumber = "0110000030",
            DateOfBirth = new DateTime(1986, 1, 1),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var patientB = new Patient
        {
            FullName = "Matching Patient B",
            Email = $"auth.match.b.{Guid.NewGuid():N}@test.local",
            PatientNumber = $"AUTH-M-B-{Guid.NewGuid():N}",
            IdNumber = $"AUTH-M-B-{Guid.NewGuid():N}",
            PhoneNumber = "0110000031",
            DateOfBirth = new DateTime(1985, 1, 1),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var doctor = new Doctor
        {
            FullName = "Matching Doctor",
            Email = $"auth.match.doctor.{Guid.NewGuid():N}@test.local",
            EmployeeNumber = $"AUTH-M-D-{Guid.NewGuid():N}",
            Specialisation = "General Practice",
            PhoneNumber = "0110000032",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await patientService.CreateAsync(patientA);
        await patientService.CreateAsync(patientB);
        await doctorService.CreateAsync(doctor);

        var appointment = new Appointment
        {
            PatientId = patientA.Id,
            DoctorId = doctor.Id,
            AppointmentDateTime =
                DateTime.UtcNow.AddDays(5),
            Reason = "Patient relationship test",
            Status = "Scheduled",
            CreatedAtUtc = DateTime.UtcNow
        };

        await appointmentService.CreateAsync(appointment);

        var controller = new DoctorController(
            doctorService,
            patientService,
            appointmentService,
            consultationService,
            prescriptionService);

        SetAuthenticatedUser(
            controller,
            doctor.Email!,
            "Doctor");

        var result =
            await controller.Consultation(
                patientB.Id,
                appointment.Id,
                "Symptoms",
                "Diagnosis",
                "Treatment",
                "Notes");

        Assert.IsType<ViewResult>(result);

        Assert.False(
            controller.ModelState.IsValid);

        Assert.Contains(
            controller.ModelState.Values,
            entry => entry.Errors.Any(error =>
                error.ErrorMessage.Contains(
                    "does not match the appointment",
                    StringComparison.OrdinalIgnoreCase)));

        Assert.Null(
            await consultationService
                .GetByAppointmentIdAsync(
                    appointment.Id));
    }

    private static void SetAuthenticatedUser(
        Controller controller,
        string email,
        string role)
    {
        var httpContext =
            new DefaultHttpContext();

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            },
            "TestAuthentication");

        httpContext.User =
            new ClaimsPrincipal(identity);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = httpContext
            };

        controller.TempData =
            new TempDataDictionary(
                httpContext,
                new InMemoryTempDataProvider());
    }

    private sealed class InMemoryTempDataProvider
        : ITempDataProvider
    {
        private readonly Dictionary<string, object?> _data = new();

        public IDictionary<string, object?> LoadTempData(
            HttpContext context)
        {
            return new Dictionary<string, object?>(_data);
        }

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object?> values)
        {
            _data.Clear();

            foreach (var value in values)
            {
                _data[value.Key] = value.Value;
            }
        }
    }
}