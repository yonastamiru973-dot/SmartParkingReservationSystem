using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

public class SlotsController : Controller
{
    private readonly IParkingSlotService _slots;
    private readonly IConfiguration _config;

    public SlotsController(IParkingSlotService slots, IConfiguration config)
    {
        _slots = slots;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, SlotType? type, SlotStatus? status)
    {
        var results = await _slots.SearchAsync(search, type, status);

        var defaultLat = double.TryParse(_config["GoogleMaps:DefaultLatitude"], out var lat) ? lat : 40.7589;
        var defaultLng = double.TryParse(_config["GoogleMaps:DefaultLongitude"], out var lng) ? lng : -73.9851;
        var defaultZoom = int.TryParse(_config["GoogleMaps:DefaultZoom"], out var zoom) ? zoom : 17;

        var vm = new SlotsIndexViewModel
        {
            Slots = results,
            SearchTerm = search,
            TypeFilter = type,
            StatusFilter = status,
            GoogleMapsApiKey = _config["GoogleMaps:ApiKey"],
            MapDefaultLatitude = defaultLat,
            MapDefaultLongitude = defaultLng,
            MapDefaultZoom = defaultZoom
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
