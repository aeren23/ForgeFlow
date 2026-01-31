using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ForgeFlow.Identity.Application.Abstractions;
using ForgeFlow.Identity.Domain.Entities;
using ForgeFlow.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ForgeFlow.Identity.Infrastructure.Services;

/// <summary>
/// JWT Token Service - Access token ve Refresh token üretimi/yönetimi
/// Secret key .env'den okunur
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public JwtTokenService(IdentityDbContext db, ILogger<JwtTokenService> logger)
    {
        _db = db;
        _logger = logger;

        // Environment variables'dan oku
        _secretKey = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set");
        _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ForgeFlow.Identity";
        _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ForgeFlow.Services";

        // Token süreleri
        _accessTokenExpirationMinutes = 60;  // 60 dakika
        _refreshTokenExpirationDays = 7;     // 7 gün
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id), // Required for UserManager.GetUserAsync
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("fullName", user.FullName ?? ""),
        };

        // Rolleri token'a ekle
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogDebug("Generated access token for user {UserId} with roles: {Roles}", user.Id, string.Join(", ", roles));

        return tokenString;
    }


    public async Task<RefreshToken> GenerateRefreshTokenAsync(ApplicationUser user, CancellationToken ct = default)
    {
        // Güvenli random token üret
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var tokenString = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            Token = tokenString,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        await _db.RefreshTokens.AddAsync(refreshToken, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogDebug("Generated refresh token for user {UserId}", user.Id);

        return refreshToken;
    }

    public async Task<(ApplicationUser? user, RefreshToken? token)> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken, ct);

        if (token == null)
        {
            _logger.LogWarning("Refresh token not found");
            return (null, null);
        }

        if (!token.IsActive)
        {
            _logger.LogWarning("Refresh token is expired or revoked for user {UserId}", token.UserId);
            return (null, null);
        }

        return (token.User, token);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken, ct);

        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogDebug("Revoked refresh token for user {UserId}", token.UserId);
        }
    }
}
