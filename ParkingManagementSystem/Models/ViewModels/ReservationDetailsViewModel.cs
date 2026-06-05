namespace ParkingManagementSystem.Models.ViewModels;

public class ReservationDetailsViewModel
{
    public Reservation Reservation { get; init; } = null!;
    public string QrCodeSvg { get; init; } = string.Empty;
    public bool QrExpired { get; init; }
    public bool CanCancel { get; init; }
    public bool CanExtend { get; init; }
    public DateTime CancelDeadline { get; init; }
}
