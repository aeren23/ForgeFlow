using ForgeFlow.Work.Domain.Enums;

namespace ForgeFlow.Work.Domain.Entities;

/// <summary>
/// Proje entity - Issue'ların gruplandığı ana yapı
/// </summary>
public class Project
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Proje kısa kodu (2-5 karakter, unique) - Issue key'lerinde kullanılır
    /// Örnek: "FORGE" → Issue key: "FORGE-123"
    /// </summary>
    public string Key { get; set; } = null!;
    
    /// <summary>
    /// Proje adı
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Proje açıklaması (markdown destekli)
    /// </summary>
    public string? Description { get; set; }
    
    // ========== Repository Entegrasyonu ==========
    
    /// <summary>
    /// Git repository URL'i - AI'nın kod analizi için
    /// </summary>
    public string? RepositoryUrl { get; set; }
    
    /// <summary>
    /// Repository sağlayıcısı (GitHub, GitLab, etc.)
    /// </summary>
    public RepositoryProvider? RepositoryProvider { get; set; }
    
    /// <summary>
    /// Varsayılan branch (main, master, develop)
    /// </summary>
    public string DefaultBranch { get; set; } = "main";
    
    // ========== AI Context ==========
    
    /// <summary>
    /// Kullanılan teknolojiler - AI prompt context'i için
    /// Örnek: ["C#", ".NET 8", "EF Core", "React"]
    /// </summary>
    public string[] TechStack { get; set; } = [];
    
    /// <summary>
    /// Proje tipi - AI'nın doğru context oluşturması için
    /// </summary>
    public ProjectType ProjectType { get; set; } = ProjectType.Backend;
    
    // ========== Ownership ==========
    
    /// <summary>
    /// Projeyi oluşturan kullanıcı (Identity Service'den)
    /// </summary>
    public string CreatorId { get; set; } = null!;
    
    // ========== Meta ==========
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Sonraki issue numarası - atomic increment için
    /// </summary>
    public int NextIssueNumber { get; set; } = 1;
    
    // ========== Navigation ==========
    
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
