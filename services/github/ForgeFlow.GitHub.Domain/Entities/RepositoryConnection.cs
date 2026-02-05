namespace ForgeFlow.GitHub.Domain.Entities;

/// <summary>
/// ForgeFlow projesi ile GitHub repository bağlantısı
/// </summary>
public class RepositoryConnection
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Work Service'deki proje ID'si
    /// </summary>
    public Guid ProjectId { get; set; }
    
    /// <summary>
    /// GitHub repository ID
    /// </summary>
    public long RepositoryId { get; set; }
    
    /// <summary>
    /// Repository tam adı: "owner/repo"
    /// </summary>
    public string FullName { get; set; } = "";
    
    /// <summary>
    /// Varsayılan branch (main, master, develop vb.)
    /// </summary>
    public string DefaultBranch { get; set; } = "main";
    
    /// <summary>
    /// Webhook secret (opsiyonel, repo-specific)
    /// </summary>
    public string? WebhookSecret { get; set; }
    
    /// <summary>
    /// Bağlı olduğu Installation
    /// </summary>
    public Guid InstallationId { get; set; }
    public GitHubInstallation Installation { get; set; } = null!;
}
