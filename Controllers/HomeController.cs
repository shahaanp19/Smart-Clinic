using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartClinicManagementSystem.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }

    [HttpGet]
    public IActionResult MedicalDivisions()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Doctors()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Dentistry()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Dietetics()
    {
        return View();
    }

    [HttpGet]
    public IActionResult OrthoticsAndProsthetics()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Physiotherapy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Podiatry()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Chatbot()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}