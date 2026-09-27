using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.Controllers;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Controllers;

public class ControllerAuthorizationTests
{
    [Fact]
    public void AdminController_RequiresAdministratorPolicy()
    {
        var attribute = GetAuthorizeAttribute<AdminController>();

        Assert.NotNull(attribute);
        Assert.Equal("AdministratorOnly", attribute!.Policy);
    }

    [Fact]
    public void DoctorController_RequiresDoctorPolicy()
    {
        var attribute = GetAuthorizeAttribute<DoctorController>();

        Assert.NotNull(attribute);
        Assert.Equal("DoctorOnly", attribute!.Policy);
    }

    [Fact]
    public void PatientController_RequiresPatientPolicy()
    {
        var attribute = GetAuthorizeAttribute<PatientController>();

        Assert.NotNull(attribute);
        Assert.Equal("PatientOnly", attribute!.Policy);
    }

    [Fact]
    public void ReceptionController_RequiresReceptionistPolicy()
    {
        var attribute = GetAuthorizeAttribute<ReceptionController>();

        Assert.NotNull(attribute);
        Assert.Equal("ReceptionistOnly", attribute!.Policy);
    }

    [Fact]
    public void AccountController_DoesNotRequireControllerLevelAuthorization()
    {
        var attributes = typeof(AccountController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true);

        Assert.Empty(attributes);
    }

    [Fact]
    public void HomeController_DoesNotRequireAuthorization()
    {
        var attributes = typeof(HomeController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true);

        Assert.Empty(attributes);
    }

    [Fact]
    public void AdminLogin_AllowsAnonymousAccess()
    {
        AssertActionAllowsAnonymous<AdminController>(
            nameof(AdminController.Login));
    }

    [Fact]
    public void DoctorLogin_AllowsAnonymousAccess()
    {
        AssertActionAllowsAnonymous<DoctorController>(
            nameof(DoctorController.Login));
    }

    [Fact]
    public void PatientLogin_AllowsAnonymousAccess()
    {
        AssertActionAllowsAnonymous<PatientController>(
            nameof(PatientController.Login));
    }

    [Fact]
    public void ReceptionLogin_AllowsAnonymousAccess()
    {
        AssertActionAllowsAnonymous<ReceptionController>(
            nameof(ReceptionController.Login));
    }

    [Fact]
    public void AccountLogin_AllowsAnonymousAccess()
    {
        var methods = GetControllerMethods<AccountController>(
            nameof(AccountController.Login));

        Assert.Contains(
            methods,
            method => method
                .GetCustomAttributes(
                    typeof(AllowAnonymousAttribute),
                    true)
                .Any());
    }

    [Fact]
    public void AccountAccessDenied_AllowsAnonymousAccess()
    {
        AssertActionAllowsAnonymous<AccountController>(
            nameof(AccountController.AccessDenied));
    }

    [Fact]
    public void AccountLogout_RequiresAuthorization()
    {
        var method = typeof(AccountController)
            .GetMethod(nameof(AccountController.Logout));

        Assert.NotNull(method);

        var attribute = method!
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
    }

    [Fact]
    public void ProtectedPostActions_UseAntiforgery()
    {
        AssertActionUsesAntiforgery<AdminController>(
            nameof(AdminController.CreateUser));

        AssertActionUsesAntiforgery<AdminController>(
            nameof(AdminController.ToggleUserStatus));

        AssertActionUsesAntiforgery<AdminController>(
            nameof(AdminController.DeleteUser));

        AssertActionUsesAntiforgery<DoctorController>(
            nameof(DoctorController.Consultation));

        AssertActionUsesAntiforgery<DoctorController>(
            nameof(DoctorController.GeneratePrescription));

        AssertActionUsesAntiforgery<PatientController>(
            nameof(PatientController.BookAppointment));

        AssertActionUsesAntiforgery<PatientController>(
            nameof(PatientController.CancelAppointment));

        AssertActionUsesAntiforgery<ReceptionController>(
            nameof(ReceptionController.RegisterPatient));

        AssertActionUsesAntiforgery<ReceptionController>(
            nameof(ReceptionController.BookAppointment));

        AssertActionUsesAntiforgery<ReceptionController>(
            nameof(ReceptionController.CancelAppointment));

        AssertActionUsesAntiforgery<AccountController>(
            nameof(AccountController.Login));

        AssertActionUsesAntiforgery<AccountController>(
            nameof(AccountController.Logout));
    }

    private static AuthorizeAttribute? GetAuthorizeAttribute<TController>()
        where TController : Controller
    {
        return typeof(TController)
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();
    }

    private static void AssertActionAllowsAnonymous<TController>(
        string actionName)
        where TController : Controller
    {
        var methods = GetControllerMethods<TController>(actionName);

        Assert.Contains(
            methods,
            method => method
                .GetCustomAttributes(
                    typeof(AllowAnonymousAttribute),
                    true)
                .Any());
    }

    private static void AssertActionUsesAntiforgery<TController>(
        string actionName)
        where TController : Controller
    {
        var methods = GetControllerMethods<TController>(actionName);

        Assert.Contains(
            methods,
            method => method
                .GetCustomAttributes(
                    typeof(ValidateAntiForgeryTokenAttribute),
                    true)
                .Any());
    }

    private static IEnumerable<System.Reflection.MethodInfo>
        GetControllerMethods<TController>(string actionName)
        where TController : Controller
    {
        return typeof(TController)
            .GetMethods()
            .Where(method => method.Name == actionName);
    }
}