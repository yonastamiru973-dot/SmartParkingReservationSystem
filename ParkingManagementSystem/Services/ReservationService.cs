using System.Data;
using Microsoft.EntityFrameworkCore;
using ParkingManagementSystem.Data;
using ParkingManagementSystem.Models;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Models.ViewModels;

namespace ParkingManagementSystem.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _db;
    private readonly IPricingService _pricing;
    private readonly IQrCodeService _qr;
    private readonly IConfiguration _config;

    public ReservationService(
        ApplicationDbContext db,
        IPricingService pricing,
        IQrCodeService qr,
        IConfiguration config)
    {
        _db = db;
        _pricing = pricing;
        _qr = qr;
        _config = config;
    }

    // ------------------------------------------------------------------- create

    public async Task<ServiceResult<Reservation>> CreateAsync(int userId, CreateReservationViewModel model)
    {
        var minMinutes = GetInt("Reservations:MinDurationMinutes", 30);
        var maxHours = GetInt("Reservations:MaxDurationHours", 24);

        if (model.DurationMinutes < minMinutes)
            return ServiceResult<Reservation>.Fail($"Minimum reservation duration is {minMinutes} minutes.");
        if (model.DurationMinutes > maxHours * 60)
            return ServiceResult<Reservation>.Fail($"Maximum reservation duration is {maxHours} hours.");

        var start = model.StartTime;
        var end = start.AddMinutes(model.DurationMinutes);

        if (start <= DateTime.Now)
            return ServiceResult<Reservation>.Fail("Start time must be in the future.");

        var slot = await _db.ParkingSlots
            .FirstOrDefaultAsync(s => s.Id == model.SlotId && !s.IsDeleted);
        if (slot == null) return ServiceResult<Reservation>.Fail("Parking slot not found.");

        if (slot.Status == SlotStatus.Maintenance)
            return ServiceResult<Reservation>.Fail("This slot is in maintenance and cannot be reserved.");

        // SERIALIZABLE transaction guarantees two concurrent overlapping bookings can't both
        // succeed: SQL Server takes range locks on the index, so the second SELECT either
        // sees the first INSERT or blocks until the first transaction commits/aborts.
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var blocking = IReservationService.BlockingStatuses;
        var overlap = await _db.Reservations
            .Where(r => r.SlotId == slot.Id
                        && blocking.Contains(r.Status)
                        && r.StartTime < end
                        && r.EndTime > start)
            .AnyAsync();

        if (overlap)
        {
            await tx.RollbackAsync();
            return ServiceResult<Reservation>.Fail("This slot is already reserved during the selected time window.");
        }

        var duration = end - start;
        var fee = _pricing.CalculateFee(slot.HourlyRate, duration);

        var reservation = new Reservation
        {
            UserId = userId,
            SlotId = slot.Id,
            StartTime = start,
            EndTime = end,
            Status = ReservationStatus.Confirmed,
            Fee = fee,
            ExtensionFee = 0m,
            ExtensionCount = 0,
            QrToken = "pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        // Now we have the Id; build the signed token using the real reservation id and store it.
        reservation.QrToken = _qr.CreateToken(reservation.Id, userId, slot.Id, start, end);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        // Eager-load slot for the result.
        reservation.Slot = slot;
        return ServiceResult<Reservation>.Ok(reservation);
    }

    // ------------------------------------------------------------------- lookup

    public Task<Reservation?> GetByIdAsync(int reservationId) =>
        _db.Reservations
            .Include(r => r.Slot)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

    public Task<Reservation?> GetByIdForUserAsync(int reservationId, int userId) =>
        _db.Reservations
            .Include(r => r.Slot)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

    public async Task<MyReservationsViewModel> GetMyReservationsAsync(int userId)
    {
        var all = await _db.Reservations
            .Include(r => r.Slot)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();

        return new MyReservationsViewModel
        {
            Active = all.Where(r => r.Status == ReservationStatus.Confirmed
                                  || r.Status == ReservationStatus.Active).ToList(),
            Past = all.Where(r => r.Status == ReservationStatus.Completed
                                || r.Status == ReservationStatus.Expired).ToList(),
            Cancelled = all.Where(r => r.Status == ReservationStatus.Cancelled).ToList()
        };
    }

    public async Task<IReadOnlyList<Reservation>> SearchAsync(
        DateTime? from, DateTime? to, ReservationStatus? status, string? userQuery)
    {
        var q = _db.Reservations
            .Include(r => r.User)
            .Include(r => r.Slot)
            .AsQueryable();

        if (from.HasValue) q = q.Where(r => r.StartTime >= from.Value);
        if (to.HasValue) q = q.Where(r => r.StartTime <= to.Value);
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(userQuery))
        {
            var term = userQuery.Trim();
            q = q.Where(r => r.User != null &&
                            (EF.Functions.Like(r.User.Email, $"%{term}%")
                          || EF.Functions.Like(r.User.FullName, $"%{term}%")
                          || EF.Functions.Like(r.User.VehiclePlateNumber, $"%{term}%")));
        }

        return await q.OrderByDescending(r => r.StartTime).ToListAsync();
    }

    // ------------------------------------------------------------------- cancel

    public async Task<ServiceResult> CancelAsync(int reservationId, int userId)
    {
        var cancelWindow = GetInt("Reservations:CancelWindowMinutes", 15);

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId && x.UserId == userId);
        if (r == null) return ServiceResult.Fail("Reservation not found.");

        if (r.Status == ReservationStatus.Cancelled) return ServiceResult.Fail("Reservation is already cancelled.");
        if (r.Status != ReservationStatus.Confirmed)
            return ServiceResult.Fail("Only upcoming reservations can be cancelled.");

        var deadline = r.StartTime.AddMinutes(-cancelWindow);
        if (DateTime.Now > deadline)
            return ServiceResult.Fail($"Reservations can only be cancelled up to {cancelWindow} minutes before the start time.");

        r.Status = ReservationStatus.Cancelled;
        r.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    // ------------------------------------------------------------------- extend

    public async Task<ServiceResult<Reservation>> ExtendAsync(int reservationId, int userId, int additionalMinutes)
    {
        if (additionalMinutes <= 0)
            return ServiceResult<Reservation>.Fail("Extension must be positive.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var r = await _db.Reservations
            .Include(x => x.Slot)
            .FirstOrDefaultAsync(x => x.Id == reservationId && x.UserId == userId);
        if (r == null) return ServiceResult<Reservation>.Fail("Reservation not found.");

        if (r.Status != ReservationStatus.Confirmed && r.Status != ReservationStatus.Active)
            return ServiceResult<Reservation>.Fail("Only confirmed or active reservations can be extended.");

        if (DateTime.Now >= r.EndTime)
            return ServiceResult<Reservation>.Fail("This reservation has already ended and cannot be extended.");

        if (r.Slot == null)
            return ServiceResult<Reservation>.Fail("Slot information unavailable for this reservation.");

        var newEnd = r.EndTime.AddMinutes(additionalMinutes);

        // Don't run into another booking on the same slot.
        var blocking = IReservationService.BlockingStatuses;
        var conflict = await _db.Reservations
            .Where(x => x.SlotId == r.SlotId
                     && x.Id != r.Id
                     && blocking.Contains(x.Status)
                     && x.StartTime < newEnd
                     && x.EndTime > r.EndTime)
            .AnyAsync();
        if (conflict)
        {
            await tx.RollbackAsync();
            return ServiceResult<Reservation>.Fail(
                "Cannot extend: another reservation starts before the new end time.");
        }

        var addFee = _pricing.CalculateFee(r.Slot.HourlyRate, TimeSpan.FromMinutes(additionalMinutes));
        r.EndTime = newEnd;
        r.ExtensionFee += addFee;
        r.ExtensionCount += 1;

        // Reissue the QR token to reflect the new end time.
        r.QrToken = _qr.CreateToken(r.Id, r.UserId, r.SlotId, r.StartTime, r.EndTime);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult<Reservation>.Ok(r);
    }

    // ------------------------------------------------------------------- scan

    public async Task<ServiceResult<Reservation>> ProcessEntryScanAsync(string token)
    {
        var grace = GetInt("Reservations:QrGracePeriodMinutes", 15);

        var payload = _qr.VerifyToken(token);
        if (payload == null)
            return ServiceResult<Reservation>.Fail("QR code is invalid or has been tampered with.");

        var r = await _db.Reservations
            .Include(x => x.Slot)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == payload.ReservationId && x.QrToken == token.Trim());

        if (r == null)
            return ServiceResult<Reservation>.Fail("Reservation not found for this QR code.");

        if (r.Status == ReservationStatus.Cancelled)
            return ServiceResult<Reservation>.Fail("This reservation was cancelled.");
        if (r.Status == ReservationStatus.Completed)
            return ServiceResult<Reservation>.Fail("This reservation is already completed.");
        if (r.Status == ReservationStatus.Expired)
            return ServiceResult<Reservation>.Fail("This QR code has expired (no entry within the grace window).");
        if (r.Status == ReservationStatus.Active)
            return ServiceResult<Reservation>.Fail($"Already checked in at {r.EntryTime?.ToLocalTime():g}.");

        var now = DateTime.Now;
        if (now > r.StartTime.AddMinutes(grace))
        {
            r.Status = ReservationStatus.Expired;
            await _db.SaveChangesAsync();
            return ServiceResult<Reservation>.Fail("This QR code has expired.");
        }

        // Mark active + entry time
        r.Status = ReservationStatus.Active;
        r.EntryTime = DateTime.UtcNow;

        // Reflect occupancy on the slot
        if (r.Slot != null && r.Slot.Status == SlotStatus.Available)
            r.Slot.Status = SlotStatus.Occupied;

        await _db.SaveChangesAsync();
        return ServiceResult<Reservation>.Ok(r);
    }

    public async Task<ServiceResult<Reservation>> ProcessExitScanAsync(string token)
    {
        var payload = _qr.VerifyToken(token);
        if (payload == null)
            return ServiceResult<Reservation>.Fail("QR code is invalid or has been tampered with.");

        var r = await _db.Reservations
            .Include(x => x.Slot)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == payload.ReservationId && x.QrToken == token.Trim());

        if (r == null)
            return ServiceResult<Reservation>.Fail("Reservation not found for this QR code.");

        if (r.Status != ReservationStatus.Active)
            return ServiceResult<Reservation>.Fail(
                $"Cannot record exit: reservation status is {r.Status}.");

        r.ExitTime = DateTime.UtcNow;
        r.ActualDuration = r.EntryTime.HasValue
            ? r.ExitTime.Value - r.EntryTime.Value
            : null;
        r.Status = ReservationStatus.Completed;

        if (r.Slot != null && r.Slot.Status == SlotStatus.Occupied)
            r.Slot.Status = SlotStatus.Available;

        await _db.SaveChangesAsync();
        return ServiceResult<Reservation>.Ok(r);
    }

    // ------------------------------------------------------------------- status sweep

    public async Task<int> AdvanceStatusesAsync(CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var grace = GetInt("Reservations:QrGracePeriodMinutes", 15);
        var expireBefore = now.AddMinutes(-grace);

        // 1) Confirmed reservations whose start + grace has passed and no entry => Expired.
        var expired = await _db.Reservations
            .Where(r => r.Status == ReservationStatus.Confirmed && r.StartTime < expireBefore)
            .ToListAsync(ct);

        foreach (var r in expired) r.Status = ReservationStatus.Expired;

        // 2) Active reservations whose EndTime has passed (user didn't exit-scan) => Completed.
        var overdue = await _db.Reservations
            .Include(r => r.Slot)
            .Where(r => r.Status == ReservationStatus.Active && r.EndTime < now)
            .ToListAsync(ct);

        foreach (var r in overdue)
        {
            r.Status = ReservationStatus.Completed;
            r.ExitTime ??= DateTime.UtcNow;
            if (r.EntryTime.HasValue) r.ActualDuration = r.ExitTime.Value - r.EntryTime.Value;
            if (r.Slot != null && r.Slot.Status == SlotStatus.Occupied)
                r.Slot.Status = SlotStatus.Available;
        }

        var changes = expired.Count + overdue.Count;
        if (changes > 0) await _db.SaveChangesAsync(ct);
        return changes;
    }

    private int GetInt(string key, int fallback) =>
        int.TryParse(_config[key], out var v) ? v : fallback;
}
