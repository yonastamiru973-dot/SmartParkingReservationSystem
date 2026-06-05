using Microsoft.EntityFrameworkCore;
using ParkingManagementSystem.Data;
using ParkingManagementSystem.Models;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;

namespace ParkingManagementSystem.Services;

public class ParkingSlotService : IParkingSlotService
{
    private readonly ApplicationDbContext _db;

    public ParkingSlotService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ParkingSlot>> SearchAsync(string? query, SlotType? type, SlotStatus? status)
    {
        var q = _db.ParkingSlots.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(s => EF.Functions.Like(s.SlotNumber, $"%{term}%"));
        }
        if (type.HasValue) q = q.Where(s => s.SlotType == type.Value);
        if (status.HasValue) q = q.Where(s => s.Status == status.Value);

        return await q.OrderBy(s => s.SlotNumber).ToListAsync();
    }

    public async Task<IReadOnlyList<ParkingSlot>> GetAllForAdminAsync() =>
        await _db.ParkingSlots
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SlotNumber)
            .ToListAsync();

    public Task<ParkingSlot?> GetByIdAsync(int id) =>
        _db.ParkingSlots.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

    public async Task<ServiceResult<ParkingSlot>> CreateAsync(ParkingSlotFormViewModel model)
    {
        var number = NormalizeNumber(model.SlotNumber);
        if (await _db.ParkingSlots.AnyAsync(s => !s.IsDeleted && s.SlotNumber == number))
            return ServiceResult<ParkingSlot>.Fail($"A slot with number '{number}' already exists.");

        var slot = new ParkingSlot
        {
            SlotNumber = number,
            SlotType = model.SlotType,
            Status = model.Status,
            HourlyRate = model.HourlyRate,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.ParkingSlots.Add(slot);
        await _db.SaveChangesAsync();
        return ServiceResult<ParkingSlot>.Ok(slot);
    }

    public async Task<ServiceResult> UpdateAsync(int id, ParkingSlotFormViewModel model)
    {
        var slot = await _db.ParkingSlots.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (slot == null) return ServiceResult.Fail("Parking slot not found.");

        var number = NormalizeNumber(model.SlotNumber);
        if (number != slot.SlotNumber)
        {
            var taken = await _db.ParkingSlots.AnyAsync(s => !s.IsDeleted && s.Id != id && s.SlotNumber == number);
            if (taken) return ServiceResult.Fail($"A slot with number '{number}' already exists.");
            slot.SlotNumber = number;
        }

        slot.SlotType = model.SlotType;
        slot.Status = model.Status;
        slot.HourlyRate = model.HourlyRate;
        slot.Latitude = model.Latitude;
        slot.Longitude = model.Longitude;
        slot.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        slot.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id)
    {
        var slot = await _db.ParkingSlots.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (slot == null) return ServiceResult.Fail("Parking slot not found.");

        slot.IsDeleted = true;
        slot.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetStatusAsync(int id, SlotStatus status)
    {
        var slot = await _db.ParkingSlots.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (slot == null) return ServiceResult.Fail("Parking slot not found.");

        slot.Status = status;
        slot.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private static string NormalizeNumber(string raw) =>
        raw.Trim().ToUpperInvariant();
}
