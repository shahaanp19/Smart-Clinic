using Microsoft.AspNetCore.Mvc;

namespace SmartClinicManagementSystem.Controllers
{
    public class ReceptionController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Username and password are required.");

                return View();
            }

            // Authentication logic can be connected to the database
            // and identity service here.
            //
            // Successful authentication should redirect to Dashboard.
            // Invalid credentials should return the Login view
            // with an appropriate error message.

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CheckIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckIn(
            string patientName,
            string patientId,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(patientName))
            {
                ModelState.AddModelError(
                    nameof(patientName),
                    "Patient name is required.");
            }

            if (string.IsNullOrWhiteSpace(patientId))
            {
                ModelState.AddModelError(
                    nameof(patientId),
                    "Patient ID is required.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            // Patient check-in and database persistence
            // should be implemented here.

            TempData["SuccessMessage"] =
                "Patient checked in successfully.";

            return RedirectToAction(nameof(Queue));
        }

        [HttpGet]
        public IActionResult Queue()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Authentication/session logout logic
            // should be implemented here.

            return RedirectToAction(
                nameof(Login));
        }
    }
}

