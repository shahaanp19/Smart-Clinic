using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace SmartClinicManagementSystem.Controllers
{
    public class PatientController : Controller
    {
        private static readonly ConcurrentDictionary<int, AppointmentRecord> Appointments = new();
        private static int _nextAppointmentId = 1;

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult BookAppointment()
        {
            ViewBag.Appointments = Appointments.Values
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookAppointment(
            string doctor,
            string medicalDivision,
            DateTime appointmentDate,
            string appointmentTime,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(doctor))
                ModelState.AddModelError("doctor", "Please select a doctor.");

            if (string.IsNullOrWhiteSpace(medicalDivision))
                ModelState.AddModelError("medicalDivision", "Please select a medical division.");

            if (appointmentDate.Date < DateTime.Today)
                ModelState.AddModelError("appointmentDate", "Appointment date cannot be in the past.");

            if (string.IsNullOrWhiteSpace(appointmentTime))
                ModelState.AddModelError("appointmentTime", "Please select an appointment time.");

            if (string.IsNullOrWhiteSpace(reason))
                ModelState.AddModelError("reason", "Please provide a reason for the appointment.");

            if (!ModelState.IsValid)
            {
                ViewBag.Appointments = Appointments.Values
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToList();

                return View();
            }

            bool slotTaken = Appointments.Values.Any(a =>
                a.Doctor.Equals(doctor, StringComparison.OrdinalIgnoreCase) &&
                a.AppointmentDate.Date == appointmentDate.Date &&
                a.AppointmentTime.Equals(appointmentTime, StringComparison.OrdinalIgnoreCase) &&
                a.Status != "Cancelled");

            if (slotTaken)
            {
                ModelState.AddModelError(
                    "appointmentTime",
                    "The selected appointment slot is no longer available.");

                ViewBag.Appointments = Appointments.Values
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToList();

                return View();
            }

            int appointmentId = Interlocked.Increment(ref _nextAppointmentId);

            var appointment = new AppointmentRecord
            {
                AppointmentId = appointmentId,
                PatientName = User.Identity?.Name ?? "Current Patient",
                Doctor = doctor,
                MedicalDivision = medicalDivision,
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
                Reason = reason,
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow
            };

            Appointments.TryAdd(appointmentId, appointment);

            TempData["SuccessMessage"] =
                $"Appointment #{appointmentId} has been successfully booked.";

            return RedirectToAction(nameof(BookAppointment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int id)
        {
            if (Appointments.TryGetValue(id, out var appointment))
            {
                appointment.Status = "Cancelled";
                TempData["SuccessMessage"] =
                    $"Appointment #{id} has been cancelled successfully.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "The selected appointment could not be found.";
            }

            return RedirectToAction(nameof(BookAppointment));
        }

        public IActionResult MedicalHistory()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }

        public IActionResult PatientIntakeForm()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public class AppointmentRecord
        {
            public int AppointmentId { get; set; }

            public string PatientName { get; set; } = string.Empty;

            public string Doctor { get; set; } = string.Empty;

            public string MedicalDivision { get; set; } = string.Empty;

            public DateTime AppointmentDate { get; set; }

            public string AppointmentTime { get; set; } = string.Empty;

            public string Reason { get; set; } = string.Empty;

            public string Status { get; set; } = "Confirmed";

            public DateTime CreatedAt { get; set; }
        }
    }
}