using Microsoft.AspNetCore.Mvc;

namespace SmartClinicManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();

        }
    
    public IActionResult UserManagement()
        {
            return View();
        }
    }
}