using Microsoft.AspNetCore.Identity;

namespace ForgeFlow.Identity.Domain.Entities;

/// <summary>
/// Uygulama kullanıcısı - ASP.NET Core Identity'den türetilmiş.
/// IdentityUser tüm authentication/authorization özelliklerini sağlar.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Kullanıcının tam adı
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Kullanıcı oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Email doğrulama tarihi (null = doğrulanmamış)
    /// İleride email doğrulama entegrasyonu için hazır
    /// </summary>
    public DateTime? EmailVerifiedAt { get; set; }
}
