namespace ForgeFlow.GitHub.Domain.Entities;

/// <summary>
/// GitHub App kurulumu (bir organization veya user'a yapılan installation)
/// </summary>
public class GitHubInstallation
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// GitHub'dan gelen installation ID
    /// </summary>
    public long InstallationId { get; set; }
    
    /// <summary>
    /// Organization veya user adı
    /// </summary>
    public string AccountLogin { get; set; } = "";
    
    /// <summary>
    /// "Organization" veya "User"
    /// </summary>
    public string AccountType { get; set; } = "";
    
    /// <summary>
    /// Cache'lenmiş access token (encrypted)
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// Token geçerlilik süresi
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }
    
    /// <summary>
    /// Kurulum tarihi
    /// </summary>
    public DateTime InstalledAtUtc { get; set; }
    
    /// <summary>
    /// Bu installation altındaki repo bağlantıları
    /// </summary>
    public ICollection<RepositoryConnection> Repositories { get; set; } = new List<RepositoryConnection>();
}
