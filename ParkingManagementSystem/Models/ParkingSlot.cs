using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ParkingManagementSystem.Models.Enums;

namespace ParkingManagementSystem.Models;

public class ParkingSlot
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string SlotNumber { get; set; } = string.Empty;

    public SlotType SlotType { get; set; } = SlotType.Standard;

    public SlotStatus Status { get; set; } = SlotStatus.Available;

    [Column(TypeName = "decimal(10,2)")]
    public decimal HourlyRate { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
