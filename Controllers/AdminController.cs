using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.Models;
using SmartClinicManagementSystem.Models.ViewModels;
using SmartClinicManagementSystem.Services.Interfaces;

namespace SmartClinicManagementSystem.Controllers;

[Authorize(Policy = "AdministratorOnly")]
public class AdminController : Controller
{
    private readonly IUserService _userService;

    private static readonly string[] AllowedRoles =
    {
        "Administrator",
        "Doctor",
        "Receptionist",
        "Patient"
    };

    public AdminController(IUserService userService)
    {
        _userService = userService;
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
        var users = await _userService.GetAllAsync();

        ViewBag.TotalUsers = users.Count;

        ViewBag.TotalDoctors = users.Count(u =>
            string.Equals(
                u.Role,
                "Doctor",
                StringComparison.OrdinalIgnoreCase));

        ViewBag.TotalReceptionists = users.Count(u =>
            string.Equals(
                u.Role,
                "Receptionist",
                StringComparison.OrdinalIgnoreCase));

        ViewBag.TotalPatients = users.Count(u =>
            string.Equals(
                u.Role,
                "Patient",
                StringComparison.OrdinalIgnoreCase));

        ViewBag.TotalAdministrators = users.Count(u =>
            string.Equals(
                u.Role,
                "Administrator",
                StringComparison.OrdinalIgnoreCase));

        ViewBag.ActiveUsers = users.Count(u => u.IsActive);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Reports()
    {
        var users = await _userService.GetAllAsync();

        ViewBag.TotalUsers = users.Count;
        ViewBag.ActiveUsers = users.Count(u => u.IsActive);

        ViewBag.Doctors = users.Count(u =>
            string.Equals(
                u.Role,
                "Doctor",
                StringComparison.OrdinalIgnoreCase));

        ViewBag.Receptionists = users.Count(u =>
            string.Equals(
                u.Role,
                "Receptionist",
                StringComparison.OrdinalIgnoreCase));

        ViewBag.Patients = users.Count(u =>
            string.Equals(
                u.Role,
                "Patient",
                StringComparison.OrdinalIgnoreCase));

        ViewBag.Administrators = users.Count(u =>
            string.Equals(
                u.Role,
                "Administrator",
                StringComparison.OrdinalIgnoreCase));

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> UserManagement()
    {
        var users = await _userService.GetAllAsync();

        ViewBag.Users = users
            .Select(user => new UserManagementViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc
            })
            .ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(
        string firstName,
        string lastName,
        string email,
        string role,
        string password)
    {
        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(role) ||
            string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] =
                "First name, last name, email, role and password are required.";

            return RedirectToAction(nameof(UserManagement));
        }

        role = role.Trim();

        if (!AllowedRoles.Any(r =>
                string.Equals(
                    r,
                    role,
                    StringComparison.OrdinalIgnoreCase)))
        {
            TempData["ErrorMessage"] =
                "The selected user role is invalid.";

            return RedirectToAction(nameof(UserManagement));
        }

        if (password.Length < 8)
        {
            TempData["ErrorMessage"] =
                "The temporary password must contain at least 8 characters.";

            return RedirectToAction(nameof(UserManagement));
        }

        var fullName = $"{firstName.Trim()} {lastName.Trim()}";

        var user = new User
        {
            FullName = fullName,
            Email = email.Trim().ToLowerInvariant(),
            Role = NormalizeRole(role),

            // IMPORTANT:
            // UserService.CreateAsync() is responsible for hashing
            // the password. Do not hash it here.
            PasswordHash = password,

            IsActive = true
        };

        try
        {
            await _userService.CreateAsync(user);

            TempData["SuccessMessage"] =
                "User created successfully.";

            return RedirectToAction(nameof(UserManagement));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;

            return RedirectToAction(nameof(UserManagement));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] =
                "Invalid user account.";

            return RedirectToAction(nameof(UserManagement));
        }

        var user = await _userService.GetByIdAsync(id);

        if (user is null)
        {
            TempData["ErrorMessage"] =
                "User account could not be found.";

            return RedirectToAction(nameof(UserManagement));
        }

        try
        {
            if (user.IsActive)
            {
                var deactivated =
                    await _userService.DeactivateAsync(id);

                if (!deactivated)
                {
                    TempData["ErrorMessage"] =
                        "User account could not be deactivated.";
                }
                else
                {
                    TempData["SuccessMessage"] =
                        "User account deactivated successfully.";
                }
            }
            else
            {
                user.IsActive = true;

                await _userService.UpdateAsync(user);

                TempData["SuccessMessage"] =
                    "User account activated successfully.";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(UserManagement));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id <= 0)
        {
            TempData["ErrorMessage"] =
                "Invalid user account.";

            return RedirectToAction(nameof(UserManagement));
        }

        var user = await _userService.GetByIdAsync(id);

        if (user is null)
        {
            TempData["ErrorMessage"] =
                "User account could not be found.";

            return RedirectToAction(nameof(UserManagement));
        }

        try
        {
            var deactivated =
                await _userService.DeactivateAsync(id);

            TempData["SuccessMessage"] = deactivated
                ? "User account deactivated successfully."
                : "User account could not be deactivated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(UserManagement));
    }

    private static string NormalizeRole(string role)
    {
        return AllowedRoles.First(r =>
            string.Equals(
                r,
                role,
                StringComparison.OrdinalIgnoreCase));
    }
}