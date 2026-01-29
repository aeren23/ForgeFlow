using ForgeFlow.Work.Application.Abstractions;

namespace ForgeFlow.Work.Api.Services;

/// <summary>
/// CurrentUserService - HttpContext'ten X-User-Id header'ını okur.
/// Gateway JWT'den claim alıp header olarak gönderir.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();

    public string? Email =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-User-Email"].FirstOrDefault();

    public IEnumerable<string> Roles
    {
        get
        {
            var rolesHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Roles"].FirstOrDefault();
            if (string.IsNullOrEmpty(rolesHeader))
                return Enumerable.Empty<string>();

            // Gateway virgülle ayırıp gönderir: "Admin,User"
            return rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
}
