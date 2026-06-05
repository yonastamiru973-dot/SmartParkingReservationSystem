using System.ComponentModel.DataAnnotations;

namespace ParkingManagementSystem.Models.ViewModels;

public class ExtendReservationViewModel
{
    public int Id { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public DateTime CurrentEndTime { get; set; }
    public decimal HourlyRate { get; set; }

    [Required]
    [Range(15, 240, ErrorMessage = "Extension must be between 15 minutes and 4 hours.")]
    [Display(Name = "Additional time (minutes)")]
    public int AdditionalMinutes { get; set; } = 30;

    public DateTime NewEndTime => CurrentEndTime.AddMinutes(AdditionalMinutes);
    public decimal AdditionalFee => Math.Round((decimal)AdditionalMinutes / 60m * HourlyRate, 2);
}
