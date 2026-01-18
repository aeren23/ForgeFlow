namespace ForgeFlow.Identity.Domain.Entities;

/// <summary>
/// Refresh Token entity - Access token yenilemek için kullanılır.
/// Her kullanıcının birden fazla refresh token'ı olabilir (farklı cihazlar).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Benzersiz token değeri (güvenli random string)
    /// </summary>
    public string Token { get; set; } = default!;

    /// <summary>
    /// Token'ın ait olduğu kullanıcı ID'si
    /// </summary>
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Token'ın geçerlilik süresi
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Token oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Token iptal edilme tarihi (null = aktif)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Token aktif mi?
    /// </summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    // Navigation property
    public ApplicationUser User { get; set; } = default!;
}
