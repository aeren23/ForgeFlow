using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using Polly;
using Polly.Retry;

namespace ForgeFlow.GitHub.Infrastructure.GitHub;

/// <summary>
/// GitHub API client factory - JWT authentication ve Installation Token yönetimi
/// </summary>
public class GitHubClientFactory : IGitHubClientFactory
{
    private readonly IConfiguration _config;
    private readonly ILogger<GitHubClientFactory> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public GitHubClientFactory(IConfiguration config, ILogger<GitHubClientFactory> logger)
    {
        _config = config;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<ApiException>(ex => ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .Or<ApiException>(ex => ex.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)));
    }

    public async Task<IGitHubClient> CreateClientAsync(long installationId)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            // 1. Base64 Private Key → PEM string
            string pemKey;
            try
            {
                var base64Key = _config["GitHub:PrivateKeyBase64"]
                    ?? throw new InvalidOperationException("GitHub:PrivateKeyBase64 not configured");
                // Sanitize base64 string
                base64Key = base64Key.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", "");
                var pemBytes = Convert.FromBase64String(base64Key);
                pemKey = Encoding.UTF8.GetString(pemBytes);

                // Log key format
                var lines = pemKey.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                _logger.LogInformation("Parsed PEM key: Length={Length}, Lines={LineCount}", pemKey.Length, lines.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode GitHub private key from Base64");
                throw;
            }

            // 2. App ID
            var appIdStr = _config["GitHub:AppId"]
                ?? throw new InvalidOperationException("GitHub:AppId not configured");

            // 3. JWT Token
            var jwtToken = CreateJwtToken(appIdStr, pemKey);

            // 4. Installation Token
            var appClient = new GitHubClient(new ProductHeaderValue("ForgeFlow"))
            {
                Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
            };

            var installationToken = await appClient.GitHubApps.CreateInstallationToken(installationId);

            _logger.LogInformation("Created installation token for {InstallationId}", installationId);

            // 5. Client
            return new GitHubClient(new ProductHeaderValue("ForgeFlow"))
            {
                Credentials = new Credentials(installationToken.Token)
            };
        });
    }

    private string CreateJwtToken(string appId, string pemKey)
    {
        // RSA parametrelerini export et, sonra RSA nesnesini hemen dispose et.
        // RsaSecurityKey'e RSAParameters (struct) ver — dispose edilecek obje yok.
        RSAParameters rsaParams;
        using (var rsa = RSA.Create())
        {
            rsa.ImportFromPem(pemKey);
            rsaParams = rsa.ExportParameters(true);
        }

        var securityKey = new RsaSecurityKey(rsaParams);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var now = DateTimeOffset.UtcNow;

        var payload = new JwtPayload
        {
            { "iat", now.AddSeconds(-60).ToUnixTimeSeconds() },
            { "exp", now.AddMinutes(5).ToUnixTimeSeconds() },
            { "iss", appId }
        };

        var header = new JwtHeader(credentials);
        var token = new JwtSecurityToken(header, payload);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
