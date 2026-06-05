using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Filters;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

[SessionAuthorize(Roles = "Admin")]
[Route("admin/slots")]
public class AdminSlotsController : Controller
{
    private readonly IParkingSlotService _slots;
    private readonly IConfiguration _config;

    public AdminSlotsController(IParkingSlotService slots, IConfiguration config)
    {
        _slots = slots;
        _config = config;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _slots.GetAllForAdminAsync();
        return View(list);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        var defaultLat = double.TryParse(_config["ParkingLot:DefaultLatitude"], out var lat) ? lat : 40.7589;
        var defaultLng = double.TryParse(_config["ParkingLot:DefaultLongitude"], out var lng) ? lng : -73.9851;

        return View(new ParkingSlotFormViewModel
        {
            HourlyRate = 5.00m,
            Latitude = defaultLat,
            Longitude = defaultLng
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ParkingSlotFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _slots.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.SlotNumber), result.Error ?? "Could not create slot.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Slot {result.Value!.SlotNumber} created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var slot = await _slots.GetByIdAsync(id);
        if (slot == null) return NotFound();

        return View(new ParkingSlotFormViewModel
        {
            Id = slot.Id,
            SlotNumber = slot.SlotNumber,
            SlotType = slot.SlotType,
            Status = slot.Status,
            HourlyRate = slot.HourlyRate,
            Latitude = slot.Latitude,
            Longitude = slot.Longitude,
            Description = slot.Description
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ParkingSlotFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var result = await _slots.UpdateAsync(id, model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update slot.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Slot updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var slot = await _slots.GetByIdAsync(id);
        if (slot == null) return NotFound();
        return View(slot);
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _slots.SoftDeleteAsync(id);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error ?? "Could not delete slot.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Slot deleted.";
        return RedirectToAction(nameof(Index));
    }

    // AJAX: change status without leaving the admin page. Used by the inline status dropdown.
    [HttpPost("set-status/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, [FromForm] SlotStatus status)
    {
        var result = await _slots.SetStatusAsync(id, status);
        if (!result.Succeeded)
            return BadRequest(new { ok = false, error = result.Error });

        return Json(new { ok = true, status = status.ToString() });
    }
}
