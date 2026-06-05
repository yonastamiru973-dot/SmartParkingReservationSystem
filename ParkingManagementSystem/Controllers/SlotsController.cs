using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

public class SlotsController : Controller
{
    private readonly IParkingSlotService _slots;

    public SlotsController(IParkingSlotService slots)
    {
        _slots = slots;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, SlotType? type, SlotStatus? status)
    {
        var results = await _slots.SearchAsync(search, type, status);

        var vm = new SlotsIndexViewModel
        {
            Slots = results,
            SearchTerm = search,
            TypeFilter = type,
            StatusFilter = status
        };
        return View(vm);
    }

    // JSON endpoint polled by the slots page to keep availability fresh without a full reload.
    [HttpGet]
    public async Task<IActionResult> Status(string? search, SlotType? type, SlotStatus? status)
    {
        var results = await _slots.SearchAsync(search, type, status);
        var payload = results.Select(s => new
        {
            id = s.Id,
            slotNumber = s.SlotNumber,
            slotType = s.SlotType.ToString(),
            status = s.Status.ToString(),
            hourlyRate = s.HourlyRate,
            latitude = s.Latitude,
            longitude = s.Longitude,
            description = s.Description
        });
        return Json(new { generatedAt = DateTime.UtcNow, slots = payload });
    }
}
