using System.ComponentModel.DataAnnotations;

namespace ParkingManagementSystem.Models.ViewModels;

public class CreateReservationViewModel
{
    [Required]
    [Display(Name = "Parking Slot")]
    public int SlotId { get; set; }

    [Required(ErrorMessage = "Start date/time is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Start time")]
    public DateTime StartTime { get; set; } = DateTime.Now.AddMinutes(5);

    [Required(ErrorMessage = "Duration is required.")]
    [Range(30, 1440, ErrorMessage = "Duration must be between 30 minutes and 24 hours.")]
    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 60;

    // ----- read-only context populated for the view -----
    public string SlotNumber { get; set; } = string.Empty;
    public string SlotType { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public string? SlotDescription { get; set; }

    public DateTime EndTime => StartTime.AddMinutes(DurationMinutes);
    public decimal EstimatedFee => Math.Round((decimal)DurationMinutes / 60m * HourlyRate, 2);
}
