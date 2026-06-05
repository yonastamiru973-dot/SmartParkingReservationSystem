using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Filters;
using ParkingManagementSystem.Models.ViewModels;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

[SessionAuthorize]
public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public ProfileController(IUserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userService.GetByIdAsync(_currentUser.UserId!.Value);
        if (user == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        var vm = new ProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            VehiclePlateNumber = user.VehiclePlateNumber,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userService.GetByIdAsync(_currentUser.UserId!.Value);
        if (user == null) return RedirectToAction("Login", "Account");

        var vm = new ProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            VehiclePlateNumber = user.VehiclePlateNumber,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.UpdateProfileAsync(_currentUser.UserId!.Value, model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update profile.");
            return View(model);
        }

        HttpContext.Session.SetString(CurrentUserService.SessionKeyEmail, model.Email.Trim().ToLowerInvariant());
        HttpContext.Session.SetString(CurrentUserService.SessionKeyFullName, model.FullName.Trim());

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
