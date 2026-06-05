using System.ComponentModel.DataAnnotations;

namespace ParkingManagementSystem.Models.ViewModels;

public enum PaymentPurpose
{
    Booking = 0,
    Extension = 1
}

public class PaymentViewModel
{
    public int ReservationId { get; set; }
    public PaymentPurpose Purpose { get; set; } = PaymentPurpose.Booking;

    /// <summary>Only set when Purpose = Extension.</summary>
    public int? AdditionalMinutes { get; set; }

    public decimal Amount { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "Card number is required.")]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "Enter a 16-digit card number (simulation only).")]
    [Display(Name = "Card number")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cardholder name is required.")]
    [StringLength(100)]
    [Display(Name = "Cardholder name")]
    public string CardHolder { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expiry is required.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Use MM/YY format.")]
    [Display(Name = "Expiry (MM/YY)")]
    public string Expiry { get; set; } = string.Empty;

    [Required(ErrorMessage = "CVV is required.")]
    [RegularExpression(@"^\d{3}$", ErrorMessage = "Enter a 3-digit CVV.")]
    [Display(Name = "CVV")]
    public string Cvv { get; set; } = string.Empty;
}
