using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Models.ViewModels;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;

    public AccountController(IUserService userService, IEmailService emailService)
    {
        _userService = userService;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return RedirectToAction("Dashboard", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.RegisterAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Account created successfully. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return RedirectToAction("Dashboard", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.ValidateLoginAsync(model.Email, model.Password);
        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Invalid email or password.");
            return View(model);
        }

        var user = result.Value;
        HttpContext.Session.SetInt32(CurrentUserService.SessionKeyUserId, user.Id);
        HttpContext.Session.SetString(CurrentUserService.SessionKeyEmail, user.Email);
        HttpContext.Session.SetString(CurrentUserService.SessionKeyFullName, user.FullName);
        HttpContext.Session.SetString(CurrentUserService.SessionKeyRole, user.Role);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Dashboard", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var tokenResult = await _userService.CreatePasswordResetTokenAsync(model.Email);
        if (!tokenResult.Succeeded || tokenResult.Value is null)
        {
            ModelState.AddModelError(string.Empty, tokenResult.Error ?? "Unable to send reset instructions.");
            return View(model);
        }

        var user = await _userService.GetByEmailAsync(model.Email);
        var resetLink = Url.Action(
            nameof(ResetPassword),
            "Account",
            new { email = model.Email, token = tokenResult.Value },
            protocol: Request.Scheme) ?? string.Empty;

        await _emailService.SendPasswordResetEmailAsync(model.Email, user?.FullName ?? string.Empty, resetLink);

        TempData["SuccessMessage"] =
            "If an account with that email exists, password reset instructions have been sent. " +
            "In development mode, the reset link is also recorded in App_Data/password-reset-emails.log.";
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "Invalid password reset link.";
            return RedirectToAction(nameof(Login));
        }
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.ResetPasswordAsync(model.Email, model.Token, model.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not reset password.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Your password has been reset. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
