using ParkingManagementSystem.Models;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;

namespace ParkingManagementSystem.Services;

public interface IParkingSlotService
{
    Task<IReadOnlyList<ParkingSlot>> SearchAsync(string? query, SlotType? type, SlotStatus? status);
    Task<IReadOnlyList<ParkingSlot>> GetAllForAdminAsync();
    Task<ParkingSlot?> GetByIdAsync(int id);

    Task<ServiceResult<ParkingSlot>> CreateAsync(ParkingSlotFormViewModel model);
    Task<ServiceResult> UpdateAsync(int id, ParkingSlotFormViewModel model);
    Task<ServiceResult> SoftDeleteAsync(int id);
    Task<ServiceResult> SetStatusAsync(int id, SlotStatus status);
}
