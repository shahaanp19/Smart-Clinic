using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace SmartClinicManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        private static readonly ConcurrentDictionary<int, ConsultationRecord> Consultations = new();
        private static readonly ConcurrentDictionary<int, PrescriptionRecord> Prescriptions = new();

        private static int _nextConsultationId = 1000;
        private static int _nextPrescriptionId = 2000;

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            ViewBag.TotalConsultations = Consultations.Count;
            ViewBag.CompletedConsultations =
                Consultations.Values.Count(c => c.Status == "Completed");
            ViewBag.PendingConsultations =
                Consultations.Values.Count(c => c.Status == "Pending");

            return View();
        }

        public IActionResult Schedule()
        {
            ViewBag.Consultations = Consultations.Values
                .OrderBy(c => c.AppointmentDate)
                .ThenBy(c => c.AppointmentTime)
                .ToList();

            return View();
        }

        [HttpGet]
        public IActionResult Consultation()
        {
            ViewBag.Consultations = Consultations.Values
                .OrderByDescending(c => c.AppointmentDate)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Consultation(
            string patientName,
            DateTime appointmentDate,
            string appointmentTime,
            string notes,
            string diagnosis)
        {
            if (string.IsNullOrWhiteSpace(patientName))
                ModelState.AddModelError("patientName", "Patient name is required.");

            if (appointmentDate.Date < DateTime.Today)
                ModelState.AddModelError(
                    "appointmentDate",
                    "Consultation date cannot be in the past.");

            if (string.IsNullOrWhiteSpace(appointmentTime))
                ModelState.AddModelError(
                    "appointmentTime",
                    "Appointment time is required.");

            if (string.IsNullOrWhiteSpace(notes))
                ModelState.AddModelError(
                    "notes",
                    "Consultation notes are required.");

            if (!ModelState.IsValid)
            {
                ViewBag.Consultations = Consultations.Values
                    .OrderByDescending(c => c.AppointmentDate)
                    .ToList();

                return View();
            }

            int id = Interlocked.Increment(ref _nextConsultationId);

            Consultations.TryAdd(id, new ConsultationRecord
            {
                ConsultationId = id,
                PatientName = patientName.Trim(),
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
                Notes = notes.Trim(),
                Diagnosis = diagnosis?.Trim() ?? string.Empty,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow
            });

            TempData["SuccessMessage"] =
                $"Consultation #{id} has been recorded successfully.";

            return RedirectToAction(nameof(Consultation));
        }

        [HttpGet]
        public IActionResult GeneratePrescription()
        {
            ViewBag.Prescriptions = Prescriptions.Values
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GeneratePrescription(
            string patientName,
            string medication,
            string dosage,
            string frequency,
            string duration,
            string instructions)
        {
            if (string.IsNullOrWhiteSpace(patientName))
                ModelState.AddModelError("patientName", "Patient name is required.");

            if (string.IsNullOrWhiteSpace(medication))
                ModelState.AddModelError("medication", "Medication is required.");

            if (string.IsNullOrWhiteSpace(dosage))
                ModelState.AddModelError("dosage", "Dosage is required.");

            if (string.IsNullOrWhiteSpace(frequency))
                ModelState.AddModelError("frequency", "Frequency is required.");

            if (string.IsNullOrWhiteSpace(duration))
                ModelState.AddModelError("duration", "Duration is required.");

            if (!ModelState.IsValid)
            {
                ViewBag.Prescriptions = Prescriptions.Values
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();

                return View();
            }

            int id = Interlocked.Increment(ref _nextPrescriptionId);

            Prescriptions.TryAdd(id, new PrescriptionRecord
            {
                PrescriptionId = id,
                PatientName = patientName.Trim(),
                Medication = medication.Trim(),
                Dosage = dosage.Trim(),
                Frequency = frequency.Trim(),
                Duration = duration.Trim(),
                Instructions = instructions?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            });

            TempData["SuccessMessage"] =
                $"Prescription #{id} has been generated successfully.";

            return RedirectToAction(nameof(GeneratePrescription));
        }

        public IActionResult MedicalRecords()
        {
            ViewBag.Consultations = Consultations.Values
                .OrderByDescending(c => c.AppointmentDate)
                .ToList();

            ViewBag.Prescriptions = Prescriptions.Values
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View();
        }

        public class ConsultationRecord
        {
            public int ConsultationId { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public DateTime AppointmentDate { get; set; }
            public string AppointmentTime { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public string Diagnosis { get; set; } = string.Empty;
            public string Status { get; set; } = "Pending";
            public DateTime CreatedAt { get; set; }
        }

        public class PrescriptionRecord
        {
            public int PrescriptionId { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string Medication { get; set; } = string.Empty;
            public string Dosage { get; set; } = string.Empty;
            public string Frequency { get; set; } = string.Empty;
            public string Duration { get; set; } = string.Empty;
            public string Instructions { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}