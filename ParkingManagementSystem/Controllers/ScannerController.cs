using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Filters;
using ParkingManagementSystem.Models.ViewModels;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

/// <summary>
/// QR scanner for parking attendants (admin role in this sprint).
/// Supports camera scan or manual token paste for entry/exit validation.
/// </summary>
[SessionAuthorize(Roles = "Admin")]
[Route("scanner")]
public class ScannerController : Controller
{
    private readonly IReservationService _reservations;

    public ScannerController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new ScannerViewModel());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ScannerViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = model.Action == ScanAction.Entry
            ? await _reservations.ProcessEntryScanAsync(model.Token)
            : await _reservations.ProcessExitScanAsync(model.Token);

        var vm = new ScanResultViewModel
        {
            Success = result.Succeeded,
            Message = result.Succeeded
                ? (model.Action == ScanAction.Entry
                    ? "Entry validated successfully."
                    : "Exit recorded successfully.")
                : (result.Error ?? "Scan failed."),
            Action = model.Action,
            Reservation = result.Value,
            ActualDuration = result.Value?.ActualDuration,
            FinalFee = result.Value?.TotalFee
        };

        ViewBag.ScanResult = vm;
        return View(model);
    }
}
