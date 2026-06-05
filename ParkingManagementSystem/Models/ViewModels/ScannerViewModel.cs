using System.ComponentModel.DataAnnotations;
using ParkingManagementSystem.Models.Enums;

namespace ParkingManagementSystem.Models.ViewModels;

public enum ScanAction
{
    Entry = 0,
    Exit = 1
}

public class ScannerViewModel
{
    [Required(ErrorMessage = "Scan or paste a QR token.")]
    [Display(Name = "QR token")]
    public string Token { get; set; } = string.Empty;

    [Display(Name = "Action")]
    public ScanAction Action { get; set; } = ScanAction.Entry;
}

public class ScanResultViewModel
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public ScanAction Action { get; init; }
    public Reservation? Reservation { get; init; }
    public TimeSpan? ActualDuration { get; init; }
    public decimal? FinalFee { get; init; }
}
