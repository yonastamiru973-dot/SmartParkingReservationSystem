using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ParkingManagementSystem.Data;
using ParkingManagementSystem.Models;
using ParkingManagementSystem.Models.ViewModels;

namespace ParkingManagementSystem.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;

    public UserService(ApplicationDbContext db, IPasswordHasher hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    public async Task<ServiceResult<User>> RegisterAsync(RegisterViewModel model)
    {
        var email = model.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists)
            return ServiceResult<User>.Fail("An account with this email address already exists.");

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            PhoneNumber = model.PhoneNumber.Trim(),
            VehiclePlateNumber = model.VehiclePlateNumber.Trim().ToUpperInvariant(),
            PasswordHash = _hasher.Hash(model.Password),
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<User>> ValidateLoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return ServiceResult<User>.Fail("Invalid email or password.");

        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user == null || !user.IsActive)
            return ServiceResult<User>.Fail("Invalid email or password.");

        if (!_hasher.Verify(password, user.PasswordHash))
            return ServiceResult<User>.Fail("Invalid email or password.");

        return ServiceResult<User>.Ok(user);
    }

    public Task<User?> GetByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync() =>
        await _db.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();

    public async Task<ServiceResult> UpdateProfileAsync(int userId, ProfileViewModel model)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return ServiceResult.Fail("User not found.");

        var newEmail = model.Email.Trim().ToLowerInvariant();
        if (newEmail != user.Email)
        {
            var taken = await _db.Users.AnyAsync(u => u.Email == newEmail && u.Id != userId);
            if (taken) return ServiceResult.Fail("That email is already in use by another account.");
            user.Email = newEmail;
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();
        user.VehiclePlateNumber = model.VehiclePlateNumber.Trim().ToUpperInvariant();
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<string>> CreatePasswordResetTokenAsync(string email)
    {
        var user = await GetByEmailAsync(email);
        if (user == null)
            return ServiceResult<string>.Fail("No account is associated with that email address.");

        // Invalidate any existing unused tokens for this user.
        var existing = await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.Used)
            .ToListAsync();
        foreach (var t in existing) t.Used = true;

        var lifetimeHours = int.TryParse(_config["AppSettings:PasswordResetTokenLifetimeHours"], out var h) ? h : 1;

        var token = GenerateSecureToken();
        var record = new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(lifetimeHours),
            Used = false
        };
        _db.PasswordResetTokens.Add(record);
        await _db.SaveChangesAsync();

        return ServiceResult<string>.Ok(token);
    }

    public async Task<ServiceResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ServiceResult.Fail("Reset token is missing.");

        var user = await GetByEmailAsync(email);
        if (user == null)
            return ServiceResult.Fail("Invalid or expired reset link.");

        var record = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.UserId == user.Id);

        if (record == null)
            return ServiceResult.Fail("Invalid or expired reset link.");

        if (!record.IsValid())
            return ServiceResult.Fail("This reset link has expired. Please request a new one.");

        user.PasswordHash = _hasher.Hash(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        record.Used = true;

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
}
