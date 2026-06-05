namespace ParkingManagementSystem.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}

public class CurrentUserService : ICurrentUserService
{
    public const string SessionKeyUserId = "auth.user.id";
    public const string SessionKeyEmail = "auth.user.email";
    public const string SessionKeyFullName = "auth.user.name";
    public const string SessionKeyRole = "auth.user.role";

    private readonly IHttpContextAccessor _accessor;
    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ISession? Session => _accessor.HttpContext?.Session;

    public int? UserId => Session?.GetInt32(SessionKeyUserId);
    public string? Email => Session?.GetString(SessionKeyEmail);
    public string? FullName => Session?.GetString(SessionKeyFullName);
    public string? Role => Session?.GetString(SessionKeyRole);
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
}
