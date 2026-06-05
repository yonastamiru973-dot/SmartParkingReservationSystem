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

        return RedirectToAction(nameof(Payment), new { id = result.Value.Id, purpose = PaymentPurpose.Booking });
    }

    [HttpGet]
    public async Task<IActionResult> Payment(int id, PaymentPurpose purpose = PaymentPurpose.Booking, int? minutes = null)
    {
        var r = await _reservations.GetByIdForUserAsync(id, _currentUser.UserId!.Value);
        if (r == null) return NotFound();

        if (purpose == PaymentPurpose.Booking)
        {
            if (r.Status != ReservationStatus.PendingPayment)
            {
                TempData["ErrorMessage"] = "This reservation has already been paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(new PaymentViewModel
            {
                ReservationId = r.Id,
                Purpose = PaymentPurpose.Booking,
                Amount = r.Fee,
                SlotNumber = r.Slot?.SlotNumber ?? string.Empty,
                StartTime = r.StartTime,
                EndTime = r.EndTime
            });
        }

        // Extension payment preview
        if (r.Status != ReservationStatus.Confirmed && r.Status != ReservationStatus.Active)
        {
            TempData["ErrorMessage"] = "This reservation cannot be extended.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (!minutes.HasValue || minutes.Value <= 0)
        {
            TempData["ErrorMessage"] = "Invalid extension duration.";
            return RedirectToAction(nameof(Extend), new { id });
        }

        var fee = await _reservations.PreviewExtensionFeeAsync(id, _currentUser.UserId!.Value, minutes.Value);
        return View(new PaymentViewModel
        {
            ReservationId = r.Id,
            Purpose = PaymentPurpose.Extension,
            AdditionalMinutes = minutes,
            Amount = fee,
            SlotNumber = r.Slot?.SlotNumber ?? string.Empty,
            StartTime = r.StartTime,
            EndTime = r.EndTime.AddMinutes(minutes.Value)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(PaymentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (model.Purpose == PaymentPurpose.Booking)
        {
            var result = await _reservations.ConfirmPaymentAsync(model.ReservationId, _currentUser.UserId!.Value);
            if (!result.Succeeded || result.Value is null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Payment could not be completed.");
                return View(model);
            }

            TempData["SuccessMessage"] =
                $"Payment successful! Reference: {result.Value.PaymentReference}. Your QR code is ready.";
            return RedirectToAction(nameof(Details), new { id = model.ReservationId });
        }

        // Extension: apply after simulated payment
        if (!model.AdditionalMinutes.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Extension duration is missing.");
            return View(model);
        }

        var ext = await _reservations.ExtendAsync(
            model.ReservationId, _currentUser.UserId!.Value, model.AdditionalMinutes.Value);

        if (!ext.Succeeded || ext.Value is null)
        {
            ModelState.AddModelError(string.Empty, ext.Error ?? "Extension payment could not be completed.");
            return View(model);
        }

        ext.Value.PaymentReference = "SIM-EXT-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        ext.Value.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Extension payment successful! Reference: {ext.Value.PaymentReference}. " +
            $"Added {model.AdditionalMinutes} minutes.";
        return RedirectToAction(nameof(Details), new { id = model.ReservationId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var r = await _reservations.GetByIdForUserAsync(id, _currentUser.UserId!.Value);
        if (r == null) return NotFound();

        if (r.Status == ReservationStatus.PendingPayment)
            return RedirectToAction(nameof(Payment), new { id, purpose = PaymentPurpose.Booking });

        var cancelWindow = int.TryParse(_config["Reservations:CancelWindowMinutes"], out var cw) ? cw : 15;
        var grace = int.TryParse(_config["Reservations:QrGracePeriodMinutes"], out var gp) ? gp : 15;
        var cancelDeadline = r.StartTime.AddMinutes(-cancelWindow);

        var qrSvg = string.Empty;
        var qrExpired = r.Status is ReservationStatus.Cancelled or ReservationStatus.Expired or ReservationStatus.Completed
                        || DateTime.Now > r.StartTime.AddMinutes(grace);
        if (!string.IsNullOrEmpty(r.QrToken) && r.QrToken is not ("pending" or "unpaid"))
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
            if (await _reservations.GetByIdForUserAsync(id, _currentUser.UserId!.Value) is { Status: ReservationStatus.PendingPayment })
                return RedirectToAction(nameof(Payment), new { id });
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

        return RedirectToAction(nameof(Payment), new
        {
            id = model.Id,
            purpose = PaymentPurpose.Extension,
            minutes = model.AdditionalMinutes
        });
    }

    private static DateTime RoundUpToNext15(DateTime t)
    {
        var minutes = t.Minute;
        var add = (15 - (minutes % 15)) % 15;
        return new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0).AddMinutes(add);
    }
}
