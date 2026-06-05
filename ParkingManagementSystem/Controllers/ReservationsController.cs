using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingManagementSystem.Data;
using ParkingManagementSystem.Filters;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

[SessionAuthorize]
public class ReservationsController : Controller
{
    private readonly IReservationService _reservations;
    private readonly ApplicationDbContext _db;
    private readonly IQrCodeService _qr;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _config;

    public ReservationsController(
        IReservationService reservations,
        ApplicationDbContext db,
        IQrCodeService qr,
        ICurrentUserService currentUser,
        IConfiguration config)
    {
        _reservations = reservations;
        _db = db;
        _qr = qr;
        _currentUser = currentUser;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = await _reservations.GetMyReservationsAsync(_currentUser.UserId!.Value);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int slotId)
    {
        var slot = await _db.ParkingSlots.FirstOrDefaultAsync(s => s.Id == slotId && !s.IsDeleted);
        if (slot == null)
        {
            TempData["ErrorMessage"] = "Parking slot not found.";
            return RedirectToAction("Index", "Slots");
        }

        var defaultStart = RoundUpToNext15(DateTime.Now.AddMinutes(15));
        var vm = new CreateReservationViewModel
        {
            SlotId = slot.Id,
            StartTime = defaultStart,
            DurationMinutes = 60,
            SlotNumber = slot.SlotNumber,
            SlotType = slot.SlotType.ToString(),
            HourlyRate = slot.HourlyRate,
            SlotDescription = slot.Description
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReservationViewModel model)
    {
        // Always re-hydrate the slot context for re-displaying the form.
        var slot = await _db.ParkingSlots.FirstOrDefaultAsync(s => s.Id == model.SlotId && !s.IsDeleted);
        if (slot != null)
        {
            model.SlotNumber = slot.SlotNumber;
            model.SlotType = slot.SlotType.ToString();
            model.HourlyRate = slot.HourlyRate;
            model.SlotDescription = slot.Description;
        }

        if (!ModelState.IsValid) return View(model);

        var result = await _reservations.CreateAsync(_currentUser.UserId!.Value, model);
        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create reservation.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Reservation confirmed for slot {result.Value.Slot?.SlotNumber}.";
        return RedirectToAction(nameof(Details), new { id = result.Value.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var r = await _reservations.GetByIdForUserAsync(id, _currentUser.UserId!.Value);
        if (r == null) return NotFound();

        var cancelWindow = int.TryParse(_config["Reservations:CancelWindowMinutes"], out var cw) ? cw : 15;
        var grace = int.TryParse(_config["Reservations:QrGracePeriodMinutes"], out var gp) ? gp : 15;
        var cancelDeadline = r.StartTime.AddMinutes(-cancelWindow);

        var qrSvg = string.Empty;
        var qrExpired = r.Status is ReservationStatus.Cancelled or ReservationStatus.Expired or ReservationStatus.Completed
                        || DateTime.Now > r.StartTime.AddMinutes(grace);
        if (!string.IsNullOrEmpty(r.QrToken) && r.QrToken != "pending")
            qrSvg = _qr.GenerateSvg(r.QrToken);

        var vm = new ReservationDetailsViewModel
        {
            Reservation = r,
            QrCodeSvg = qrSvg,
            QrExpired = qrExpired,
            CanCancel = r.Status == ReservationStatus.Confirmed && DateTime.Now <= cancelDeadline,
            CanExtend = (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Active)
                        && DateTime.Now < r.EndTime,
            CancelDeadline = cancelDeadline
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
    {
        var r = await _reservations.GetByIdForUserAsync(id, _currentUser.UserId!.Value);
        if (r == null) return NotFound();
        return View(r);
    }

    [HttpPost, ActionName("Cancel"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelConfirmed(int id)
    {
        var result = await _reservations.CancelAsync(id, _currentUser.UserId!.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error ?? "Could not cancel reservation.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["SuccessMessage"] = "Reservation cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Extend(int id)
    {
        var r = await _reservations.GetByIdForUserAsync(id, _currentUser.UserId!.Value);
        if (r == null) return NotFound();

        if (r.Status != ReservationStatus.Confirmed && r.Status != ReservationStatus.Active)
        {
            TempData["ErrorMessage"] = "Only confirmed or active reservations can be extended.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (DateTime.Now >= r.EndTime)
        {
            TempData["ErrorMessage"] = "This reservation has already ended.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new ExtendReservationViewModel
        {
            Id = r.Id,
            SlotNumber = r.Slot?.SlotNumber ?? string.Empty,
            CurrentEndTime = r.EndTime,
            HourlyRate = r.Slot?.HourlyRate ?? 0m,
            AdditionalMinutes = 30
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Extend(ExtendReservationViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _reservations.ExtendAsync(model.Id, _currentUser.UserId!.Value, model.AdditionalMinutes);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not extend reservation.");
            // re-hydrate display fields
            var r = await _reservations.GetByIdForUserAsync(model.Id, _currentUser.UserId!.Value);
            if (r != null)
            {
                model.SlotNumber = r.Slot?.SlotNumber ?? string.Empty;
                model.CurrentEndTime = r.EndTime;
                model.HourlyRate = r.Slot?.HourlyRate ?? 0m;
            }
            return View(model);
        }

        TempData["SuccessMessage"] = $"Reservation extended by {model.AdditionalMinutes} minutes.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    private static DateTime RoundUpToNext15(DateTime t)
    {
        var minutes = t.Minute;
        var add = (15 - (minutes % 15)) % 15;
        return new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0).AddMinutes(add);
    }
}
