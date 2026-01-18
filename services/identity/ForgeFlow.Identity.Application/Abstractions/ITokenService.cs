using ForgeFlow.Identity.Domain.Entities;

namespace ForgeFlow.Identity.Application.Abstractions;

/// <summary>
/// JWT token üretimi ve yönetimi için servis interface'i.
/// Infrastructure katmanında implement edilir.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Kullanıcı için access token üretir (roller dahil)
    /// </summary>
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);

    /// <summary>
    /// Yeni bir refresh token üretir ve veritabanına kaydeder
    /// </summary>
    Task<RefreshToken> GenerateRefreshTokenAsync(ApplicationUser user, CancellationToken ct = default);

    /// <summary>
    /// Refresh token'ı doğrular ve kullanıcıyı döner
    /// </summary>
    Task<(ApplicationUser? user, RefreshToken? token)> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Refresh token'ı iptal eder
    /// </summary>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}

