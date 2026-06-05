using ParkingManagementSystem.Models.Enums;

namespace ParkingManagementSystem.Models.ViewModels;

public class AdminReservationsViewModel
{
    public IReadOnlyList<Reservation> Reservations { get; init; } = Array.Empty<Reservation>();

    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public ReservationStatus? Status { get; init; }
    public string? UserQuery { get; init; }

    public int Total => Reservations.Count;
    public int ConfirmedCount => Reservations.Count(r => r.Status == ReservationStatus.Confirmed);
    public int ActiveCount    => Reservations.Count(r => r.Status == ReservationStatus.Active);
    public int CompletedCount => Reservations.Count(r => r.Status == ReservationStatus.Completed);
    public int CancelledCount => Reservations.Count(r => r.Status == ReservationStatus.Cancelled);
    public int ExpiredCount   => Reservations.Count(r => r.Status == ReservationStatus.Expired);
    public int PendingPaymentCount => Reservations.Count(r => r.Status == ReservationStatus.PendingPayment);
}
