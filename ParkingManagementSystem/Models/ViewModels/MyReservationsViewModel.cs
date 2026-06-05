namespace ParkingManagementSystem.Models.ViewModels;

public class MyReservationsViewModel
{
    public IReadOnlyList<Reservation> Active { get; init; } = Array.Empty<Reservation>();
    public IReadOnlyList<Reservation> Past { get; init; } = Array.Empty<Reservation>();
    public IReadOnlyList<Reservation> Cancelled { get; init; } = Array.Empty<Reservation>();
}
