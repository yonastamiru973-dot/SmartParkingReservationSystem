using System.ComponentModel.DataAnnotations;

namespace ParkingManagementSystem.Models.ViewModels;

public class ProfileViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle plate number is required.")]
    [StringLength(20, MinimumLength = 2)]
    [Display(Name = "Vehicle Plate Number")]
    public string VehiclePlateNumber { get; set; } = string.Empty;

    [Display(Name = "Role")]
    public string Role { get; set; } = "User";

    [Display(Name = "Member Since")]
    public DateTime CreatedAt { get; set; }
}
