using ParkingManagementSystem.Models;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;

namespace ParkingManagementSystem.Services;

public interface IReservationService
{
    /// <summary>Returns the active statuses that occupy a slot (block new overlapping bookings).</summary>
    static readonly ReservationStatus[] BlockingStatuses =
    {
        ReservationStatus.Confirmed,
        ReservationStatus.Active
    };

    Task<ServiceResult<Reservation>> CreateAsync(int userId, CreateReservationViewModel model);
    Task<Reservation?> GetByIdForUserAsync(int reservationId, int userId);
    Task<Reservation?> GetByIdAsync(int reservationId);

    Task<MyReservationsViewModel> GetMyReservationsAsync(int userId);
    Task<IReadOnlyList<Reservation>> SearchAsync(DateTime? from, DateTime? to, ReservationStatus? status, string? userQuery);

    Task<ServiceResult> CancelAsync(int reservationId, int userId);
    Task<ServiceResult<Reservation>> ExtendAsync(int reservationId, int userId, int additionalMinutes);

    Task<ServiceResult<Reservation>> ProcessEntryScanAsync(string token);
    Task<ServiceResult<Reservation>> ProcessExitScanAsync(string token);

    /// <summary>Sweeps overdue Confirmed/Active reservations to Expired/Completed.</summary>
    Task<int> AdvanceStatusesAsync(CancellationToken ct = default);
}
