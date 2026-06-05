using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace ParkingManagementSystem.Services;

public class QrCodeService : IQrCodeService
{
    private const string Prefix = "RES";
    private readonly byte[] _key;

    public QrCodeService(IConfiguration config)
    {
        var secret = config["Reservations:QrSecret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            // Fall back to a deterministic dev key so the app boots cleanly during development.
            // Production deployments MUST set Reservations:QrSecret to a long random value.
            secret = "DEV-FALLBACK-INSECURE-QR-SECRET-PLEASE-CHANGE-IN-PRODUCTION-123456";
        }
        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string CreateToken(int reservationId, int userId, int slotId, DateTime startTime, DateTime endTime)
    {
        var payload = $"{Prefix}|{reservationId}|{userId}|{slotId}|{startTime.Ticks}|{endTime.Ticks}";
        var sig = ComputeSignature(payload);
        return $"{payload}|{sig}";
    }

    public QrPayload? VerifyToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var trimmed = token.Trim();
        var parts = trimmed.Split('|');
        if (parts.Length != 7 || parts[0] != Prefix) return null;

        var payload = string.Join('|', parts.Take(6));
        var expected = ComputeSignature(payload);
        if (!FixedTimeEquals(expected, parts[6])) return null;

        if (!int.TryParse(parts[1], out var resId)) return null;
        if (!int.TryParse(parts[2], out var userId)) return null;
        if (!int.TryParse(parts[3], out var slotId)) return null;
        if (!long.TryParse(parts[4], out var startTicks)) return null;
        if (!long.TryParse(parts[5], out var endTicks)) return null;

        return new QrPayload(
            resId, userId, slotId,
            new DateTime(startTicks),
            new DateTime(endTicks));
    }

    public string GenerateSvg(string content, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data);
        return svg.GetGraphic(pixelsPerModule, "#1f2937", "#ffffff");
    }

    private string ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        if (bytesA.Length != bytesB.Length) return false;
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
