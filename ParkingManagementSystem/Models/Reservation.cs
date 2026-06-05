using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ParkingManagementSystem.Models.Enums;

namespace ParkingManagementSystem.Models;

public class Reservation
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int SlotId { get; set; }
    public ParkingSlot? Slot { get; set; }

    /// <summary>Planned start time (local).</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Planned end time (local).</summary>
    public DateTime EndTime { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

    /// <summary>Base fee at booking time = duration in hours * slot hourly rate.</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Fee { get; set; }

    /// <summary>Total of any time-extension top-ups paid after the initial reservation.</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal ExtensionFee { get; set; }

    public int ExtensionCount { get; set; }

    /// <summary>Self-contained signed QR token (encoded into the QR image).</summary>
    [MaxLength(256)]
    public string QrToken { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>When the simulated payment was completed (no real gateway).</summary>
    public DateTime? PaidAt { get; set; }

    [MaxLength(50)]
    public string? PaymentReference { get; set; }

    /// <summary>Actual time spent in the slot (set on exit scan or auto-completion).</summary>
    public TimeSpan? ActualDuration { get; set; }

    [NotMapped]
    public TimeSpan PlannedDuration => EndTime - StartTime;

    [NotMapped]
    public decimal TotalFee => Fee + ExtensionFee;
}
