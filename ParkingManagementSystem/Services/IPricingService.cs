namespace ParkingManagementSystem.Services;

public interface IPricingService
{
    decimal CalculateFee(decimal hourlyRate, TimeSpan duration);
}

public class PricingService : IPricingService
{
    public decimal CalculateFee(decimal hourlyRate, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return 0m;
        var hours = (decimal)duration.TotalHours;
        return Math.Round(hourlyRate * hours, 2, MidpointRounding.AwayFromZero);
    }
}
