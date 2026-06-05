using Microsoft.AspNetCore.Mvc;
using ParkingManagementSystem.Filters;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Controllers;

[SessionAuthorize(Roles = "Admin")]
[Route("admin/reservations")]
public class AdminReservationsController : Controller
{
    private readonly IReservationService _reservations;

    public AdminReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, ReservationStatus? status, string? user)
    {
        var results = await _reservations.SearchAsync(from, to, status, user);

        var vm = new AdminReservationsViewModel
        {
            Reservations = results,
            FromDate = from,
            ToDate = to,
            Status = status,
            UserQuery = user
        };
        return View(vm);
    }
}
