using ParkingManagementSystem.Models;
using ParkingManagementSystem.Models.ViewModels;

namespace ParkingManagementSystem.Services;

public class ServiceResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public static ServiceResult Ok() => new() { Succeeded = true };
    public static ServiceResult Fail(string error) => new() { Succeeded = false, Error = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }
    public static ServiceResult<T> Ok(T value) => new() { Succeeded = true, Value = value };
    public new static ServiceResult<T> Fail(string error) => new() { Succeeded = false, Error = error };
}

public interface IUserService
{
    Task<ServiceResult<User>> RegisterAsync(RegisterViewModel model);
    Task<ServiceResult<User>> ValidateLoginAsync(string email, string password);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<ServiceResult> UpdateProfileAsync(int userId, ProfileViewModel model);

    Task<ServiceResult<User>> CreateAsAdminAsync(AdminUserFormViewModel model);
    Task<ServiceResult> UpdateAsAdminAsync(int userId, AdminUserFormViewModel model, int actingAdminId);
    Task<ServiceResult> DeleteUserAsync(int userId, int actingAdminId);

    Task<ServiceResult<string>> CreatePasswordResetTokenAsync(string email);
    Task<ServiceResult> ResetPasswordAsync(string email, string token, string newPassword);
}
