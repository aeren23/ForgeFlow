namespace ForgeFlow.Work.Application.Abstractions;

/// <summary>
/// ICurrentUserService - Gateway'den gelen X-User-Id header'ını okur.
/// Interface Application katmanında, implementasyon API katmanında olmalıdır.
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
    /// Kullanıcı rolleri (Gateway X-User-Roles header'ından)
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Belirtilen role sahip mi?
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Kullanıcı authenticate olmuş mu?
    /// </summary>
    bool IsAuthenticated { get; }
}
