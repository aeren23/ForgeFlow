namespace ForgeFlow.Work.Api.Services;

/// <summary>
/// ICurrentUserService - Gateway'den gelen X-User-Id header'ını okur.
/// Strict Clean Architecture: Controller bu interface'i kullanır.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Mevcut kullanıcının ID'si (Gateway X-User-Id header'ından)
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Mevcut kullanıcının email'i (Gateway X-User-Email header'ından)
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Kullanıcı authenticate olmuş mu?
    /// </summary>
    bool IsAuthenticated { get; }
}

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

    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
}
