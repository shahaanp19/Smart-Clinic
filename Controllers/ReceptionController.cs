using Microsoft.AspNetCore.Mvc;

namespace SmartClinicManagementSystem.Controllers
{
    public class ReceptionController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult CheckIn()
        {
            return View();
        }

        public IActionResult Queue()
        {
            return View();
        }
    }
}