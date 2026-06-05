using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Filters;
using ParkingManagementSystem.Models;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly ICurrentUserService _currentUser;

    public HomeController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public IActionResult Index()
    {
        if (_currentUser.IsAuthenticated)
            return RedirectToAction(nameof(Dashboard));
        return View();
    }

    [SessionAuthorize]
    public IActionResult Dashboard()
    {
        ViewData["FullName"] = _currentUser.FullName;
        ViewData["Email"] = _currentUser.Email;
        ViewData["Role"] = _currentUser.Role;
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
