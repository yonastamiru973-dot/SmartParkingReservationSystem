using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Filters;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

[SessionAuthorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;

    public AdminController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Users()
    {
        var users = await _userService.GetAllAsync();
        return View(users);
    }
}
