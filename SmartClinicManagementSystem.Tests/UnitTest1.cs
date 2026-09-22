using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SmartClinicManagementSystem.Controllers;
using Xunit;

namespace SmartClinicManagementSystem.Tests;

public class TestTempDataProvider : ITempDataProvider
{
    private readonly Dictionary<string, object?> _data = new();

    public IDictionary<string, object?> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object?>(_data);
    }

    public void SaveTempData(
        HttpContext context,
        IDictionary<string, object?> values)
    {
        _data.Clear();

        foreach (var item in values)
        {
            _data[item.Key] = item.Value;
        }
    }
}

public static class ControllerTestHelper
{
    public static void SetupController(Controller controller)
    {
        var httpContext = new DefaultHttpContext();

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, "Test Patient")
            },
            authenticationType: "TestAuthentication");

        httpContext.User = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        controller.TempData = new TempDataDictionary(
            httpContext,
            new TestTempDataProvider());
    }
}

public class AdminControllerTests
{
    [Fact]
    public void Dashboard_ReturnsViewWithUserStatistics()
    {
        var controller = new AdminController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.Dashboard();

        Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewBag.TotalUsers);
        Assert.NotNull(controller.ViewBag.TotalDoctors);
        Assert.NotNull(controller.ViewBag.TotalReceptionists);
        Assert.NotNull(controller.ViewBag.TotalPatients);
    }

    [Fact]
    public void CreateUser_WithMissingRequiredFields_ReturnsToUserManagement()
    {
        var controller = new AdminController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.CreateUser(
            "",
            "Smith",
            "missing@example.com",
            "Doctor");

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(
            nameof(AdminController.UserManagement),
            redirect.ActionName);

        Assert.Equal(
            "All user details are required.",
            controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public void CreateUser_WithValidDetails_RedirectsToUserManagement()
    {
        var controller = new AdminController();
        ControllerTestHelper.SetupController(controller);

        var uniqueEmail =
            $"doctor-{Guid.NewGuid():N}@example.com";

        var result = controller.CreateUser(
            "John",
            "Smith",
            uniqueEmail,
            "Doctor");

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(
            nameof(AdminController.UserManagement),
            redirect.ActionName);

        Assert.Equal(
            "User created successfully.",
            controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public void CreateUser_WithDuplicateEmail_IsRejected()
    {
        var controller = new AdminController();
        ControllerTestHelper.SetupController(controller);

        var uniqueEmail =
            $"duplicate-{Guid.NewGuid():N}@example.com";

        controller.CreateUser(
            "John",
            "Smith",
            uniqueEmail,
            "Doctor");

        var result = controller.CreateUser(
            "Jane",
            "Smith",
            uniqueEmail,
            "Patient");

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(
            nameof(AdminController.UserManagement),
            redirect.ActionName);

        Assert.Equal(
            "A user with this email address already exists.",
            controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public void DeleteUser_WithUnknownId_ReturnsError()
    {
        var controller = new AdminController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.DeleteUser(int.MaxValue);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(
            nameof(AdminController.UserManagement),
            redirect.ActionName);

        Assert.Equal(
            "User account could not be found.",
            controller.TempData["ErrorMessage"]);
    }
}

public class DoctorControllerTests
{
    [Fact]
    public void Dashboard_ReturnsViewWithConsultationStatistics()
    {
        var controller = new DoctorController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.Dashboard();

        Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewBag.TotalConsultations);
        Assert.NotNull(controller.ViewBag.CompletedConsultations);
        Assert.NotNull(controller.ViewBag.PendingConsultations);
    }

    [Fact]
    public void Consultation_WithMissingPatientName_ReturnsViewWithValidationError()
    {
        var controller = new DoctorController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.Consultation(
            "",
            DateTime.Today.AddDays(1),
            "10:00",
            "Patient consultation notes",
            "Diagnosis");

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        Assert.Contains(
            controller.ModelState["patientName"]!.Errors,
            error => error.ErrorMessage ==
                     "Patient name is required.");
    }

    [Fact]
    public void Consultation_WithPastDate_ReturnsViewWithValidationError()
    {
        var controller = new DoctorController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.Consultation(
            "John Smith",
            DateTime.Today.AddDays(-1),
            "10:00",
            "Patient consultation notes",
            "Diagnosis");

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        Assert.Contains(
            controller.ModelState["appointmentDate"]!.Errors,
            error => error.ErrorMessage ==
                     "Consultation date cannot be in the past.");
    }

    [Fact]
    public void Consultation_WithValidDetails_RedirectsToConsultation()
    {
        var controller = new DoctorController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.Consultation(
            $"Test Patient {Guid.NewGuid():N}",
            DateTime.Today.AddDays(10),
            "11:00",
            "Patient reports improvement.",
            "Routine follow-up");

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(
            nameof(DoctorController.Consultation),
            redirect.ActionName);

        Assert.NotNull(controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public void GeneratePrescription_WithMissingMedication_ReturnsValidationError()
    {
        var controller = new DoctorController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.GeneratePrescription(
            "John Smith",
            "",
            "500mg",
            "Twice daily",
            "7 days",
            "Take after meals.");

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        Assert.Contains(
            controller.ModelState["medication"]!.Errors,
            error => error.ErrorMessage ==
                     "Medication is required.");
    }
}

public class PatientControllerTests
{
    [Fact]
    public void BookAppointment_WithMissingDoctor_ReturnsValidationError()
    {
        var controller = new PatientController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.BookAppointment(
            "",
            "General Medicine",
            DateTime.Today.AddDays(5),
            "09:00",
            "Routine consultation");

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        Assert.Contains(
            controller.ModelState["doctor"]!.Errors,
            error => error.ErrorMessage ==
                     "Please select a doctor.");
    }

    [Fact]
    public void BookAppointment_WithPastDate_ReturnsValidationError()
    {
        var controller = new PatientController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.BookAppointment(
            "Dr Smith",
            "General Medicine",
            DateTime.Today.AddDays(-1),
            "09:00",
            "Routine consultation");

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        Assert.Contains(
            controller.ModelState["appointmentDate"]!.Errors,
            error => error.ErrorMessage ==
                     "Appointment date cannot be in the past.");
    }

    [Fact]
    public void BookAppointment_WithValidDetails_RedirectsToBookAppointment()
    {
        var controller = new PatientController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.BookAppointment(
            $"Dr-{Guid.NewGuid():N}",
            "General Medicine",
            DateTime.Today.AddDays(20),
            "14:00",
            "Routine consultation");

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(
            nameof(PatientController.BookAppointment),
            redirect.ActionName);

        Assert.NotNull(controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public void CancelAppointment_WithUnknownId_ReturnsError()
    {
        var controller = new PatientController();
        ControllerTestHelper.SetupController(controller);

        var result = controller.CancelAppointment(int.MaxValue);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(
            nameof(PatientController.BookAppointment),
            redirect.ActionName);

        Assert.Equal(
            "The selected appointment could not be found.",
            controller.TempData["ErrorMessage"]);
    }
}
