using System.Net;
using System.Net.Mail;

namespace ParkingManagementSystem.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _env;

    public EmailService(IConfiguration config, ILogger<EmailService> logger, IWebHostEnvironment env)
    {
        _config = config;
        _logger = logger;
        _env = env;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var mode = _config["EmailSettings:Mode"] ?? "Development";

        if (string.Equals(mode, "Development", StringComparison.OrdinalIgnoreCase))
        {
            // In Development, log the reset link and write it to a local file so the user
            // can see it without configuring an SMTP server. This satisfies AC1.7 / T1.9
            // without requiring a real mail server during local testing.
            _logger.LogWarning("[DEV EMAIL] Password reset for {Email}: {ResetLink}", toEmail, resetLink);

            try
            {
                var dir = Path.Combine(_env.ContentRootPath, "App_Data");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "password-reset-emails.log");
                var content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] To: {toEmail} ({toName})\nLink: {resetLink}\n\n";
                await File.AppendAllTextAsync(path, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write development email log.");
            }
            return;
        }

        var host = _config["EmailSettings:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogError("SMTP host not configured.");
            return;
        }

        var port = int.TryParse(_config["EmailSettings:SmtpPort"], out var p) ? p : 587;
        var user = _config["EmailSettings:SmtpUser"];
        var pass = _config["EmailSettings:SmtpPassword"];
        var enableSsl = bool.TryParse(_config["EmailSettings:EnableSsl"], out var ssl) && ssl;
        var fromAddress = _config["EmailSettings:FromAddress"] ?? "no-reply@parking.local";
        var fromName = _config["EmailSettings:FromName"] ?? "Parking Management System";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(user, pass)
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = "Reset your password",
            Body = BuildEmailBody(toName, resetLink),
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail, toName));

        await client.SendMailAsync(message);
    }

    private static string BuildEmailBody(string toName, string resetLink) => $@"
        <p>Hello {WebUtility.HtmlEncode(toName)},</p>
        <p>We received a request to reset your password for the Parking Management System.</p>
        <p>Click the link below to reset your password. This link will expire in 1 hour.</p>
        <p><a href=""{resetLink}"">Reset Password</a></p>
        <p>If you did not request this, you can safely ignore this email.</p>
        <p>— Parking Management System</p>";
}
