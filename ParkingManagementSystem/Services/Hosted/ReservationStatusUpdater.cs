namespace ParkingManagementSystem.Services.Hosted;

/// <summary>
/// Periodically sweeps the database to transition reservations between statuses
/// without waiting for a user action (e.g. Confirmed → Expired when nobody scanned
/// the QR within the grace window, Active → Completed when EndTime passes).
/// </summary>
public class ReservationStatusUpdater : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _sp;
    private readonly ILogger<ReservationStatusUpdater> _logger;

    public ReservationStatusUpdater(IServiceProvider sp, ILogger<ReservationStatusUpdater> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationStatusUpdater started; interval {Interval}.", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var changed = await svc.AdvanceStatusesAsync(stoppingToken);
                if (changed > 0)
                    _logger.LogInformation("Auto-updated {Count} reservation status(es).", changed);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReservationStatusUpdater sweep failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }
}
