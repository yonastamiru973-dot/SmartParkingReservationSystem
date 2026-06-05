using ParkingManagementSystem.Models.Enums;

namespace ParkingManagementSystem.Models.ViewModels;

public class SlotsIndexViewModel
{
    public IReadOnlyList<ParkingSlot> Slots { get; init; } = Array.Empty<ParkingSlot>();
    public string? SearchTerm { get; init; }
    public SlotType? TypeFilter { get; init; }
    public SlotStatus? StatusFilter { get; init; }

    public int TotalCount => Slots.Count;
    public int AvailableCount => Slots.Count(s => s.Status == SlotStatus.Available);
    public int OccupiedCount => Slots.Count(s => s.Status == SlotStatus.Occupied);
    public int MaintenanceCount => Slots.Count(s => s.Status == SlotStatus.Maintenance);
}
