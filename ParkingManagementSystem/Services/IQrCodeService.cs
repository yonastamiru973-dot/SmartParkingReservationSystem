namespace ParkingManagementSystem.Services;

public record QrPayload(int ReservationId, int UserId, int SlotId, DateTime StartTime, DateTime EndTime);

public interface IQrCodeService
{
    /// <summary>Builds a self-contained HMAC-signed token for a reservation.</summary>
    string CreateToken(int reservationId, int userId, int slotId, DateTime startTime, DateTime endTime);

    /// <summary>Parses + verifies the HMAC of a token. Returns null when tampered or malformed.</summary>
    QrPayload? VerifyToken(string token);

    /// <summary>Renders the supplied content as an inline SVG QR code.</summary>
    string GenerateSvg(string content, int pixelsPerModule = 6);
}
