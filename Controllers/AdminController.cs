using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace SmartClinicManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private static readonly ConcurrentDictionary<int, UserRecord> Users = new();
        private static int _nextUserId = 1000;

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            ViewBag.TotalUsers = Users.Count;
            ViewBag.TotalDoctors = Users.Values.Count(u => u.Role == "Doctor");
            ViewBag.TotalReceptionists = Users.Values.Count(u => u.Role == "Receptionist");
            ViewBag.TotalPatients = Users.Values.Count(u => u.Role == "Patient");

            return View();
        }

        public IActionResult Reports()
        {
            ViewBag.TotalUsers = Users.Count;
            ViewBag.ActiveUsers = Users.Values.Count(u => u.IsActive);
            ViewBag.Doctors = Users.Values.Count(u => u.Role == "Doctor");
            ViewBag.Receptionists = Users.Values.Count(u => u.Role == "Receptionist");
            ViewBag.Patients = Users.Values.Count(u => u.Role == "Patient");

            return View();
        }

        [HttpGet]
        public IActionResult UserManagement()
        {
            ViewBag.Users = Users.Values
                .OrderBy(u => u.Role)
                .ThenBy(u => u.LastName)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(
            string firstName,
            string lastName,
            string email,
            string role)
        {
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(role))
            {
                TempData["ErrorMessage"] = "All user details are required.";
                return RedirectToAction(nameof(UserManagement));
            }

            if (Users.Values.Any(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["ErrorMessage"] = "A user with this email address already exists.";
                return RedirectToAction(nameof(UserManagement));
            }

            int id = Interlocked.Increment(ref _nextUserId);

            Users.TryAdd(id, new UserRecord
            {
                UserId = id,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = email.Trim(),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            TempData["SuccessMessage"] = "User created successfully.";

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleUserStatus(int id)
        {
            if (Users.TryGetValue(id, out var user))
            {
                user.IsActive = !user.IsActive;

                TempData["SuccessMessage"] =
                    user.IsActive
                        ? "User account activated successfully."
                        : "User account deactivated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User account could not be found.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (Users.TryRemove(id, out _))
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User account could not be found.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        public class UserRecord
        {
            public int UserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}