using Microsoft.AspNetCore.Mvc;
using SmartClinicManagementSystem.Models;
using System.Diagnostics;

namespace SmartClinicManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Landing page
        public IActionResult Index()
        {
            return View();
        }

        // About page
        public IActionResult About()
        {
            return View();
        }

        // Contact page
        public IActionResult Contact()
        {
            return View();
        }

        // Medical Divisions page
        public IActionResult MedicalDivisions()
        {
            return View();
        }

        // Existing privacy page
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Chatbot()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]

        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
                });
        }
    }
}