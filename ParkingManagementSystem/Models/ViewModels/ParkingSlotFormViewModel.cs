using System.ComponentModel.DataAnnotations;
using ParkingManagementSystem.Models.Enums;

namespace ParkingManagementSystem.Models.ViewModels;

public class ParkingSlotFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Slot number is required.")]
    [StringLength(20, MinimumLength = 1)]
    [Display(Name = "Slot Number")]
    public string SlotNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Slot Type")]
    public SlotType SlotType { get; set; } = SlotType.Standard;

    [Required]
    [Display(Name = "Status")]
    public SlotStatus Status { get; set; } = SlotStatus.Available;

    [Required(ErrorMessage = "Hourly rate is required.")]
    [Range(0.01, 9999.99, ErrorMessage = "Hourly rate must be greater than 0.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Hourly Rate")]
    public decimal HourlyRate { get; set; } = 5.00m;

    [Required(ErrorMessage = "Latitude is required.")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    [Display(Name = "Latitude")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required.")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    [Display(Name = "Longitude")]
    public double Longitude { get; set; }

    [StringLength(500)]
    [Display(Name = "Description (optional)")]
    public string? Description { get; set; }
}
