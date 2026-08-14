using Microsoft.AspNetCore.Mvc;

namespace SmartClinicManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Schedule()
        {
            return View();
        }

        public IActionResult Consultation()
        {
            return View();
        }

        public IActionResult GeneratePrescription()
        {
            return View();
        }

        public IActionResult MedicalRecords()
        {
            return View();
        }
    }
}