using System.ComponentModel.DataAnnotations;

namespace ParkingManagementSystem.Models;

public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Used { get; set; }

    public bool IsValid() => !Used && DateTime.UtcNow <= ExpiresAt;
}
