using ForgeFlow.Work.Domain.Enums;

namespace ForgeFlow.Work.Domain.Entities;

/// <summary>
/// Issue entity - task, bug, feature, story veya epic
/// </summary>
public class Issue
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Issue benzersiz anahtarı - auto-generated
    /// Format: "{ProjectKey}-{Number}" → "FORGE-123"
    /// </summary>
    public string Key { get; set; } = null!;
    
    /// <summary>
    /// Issue başlığı
    /// </summary>
    public string Title { get; set; } = null!;
    
    /// <summary>
    /// Issue detaylı açıklaması (markdown destekli)
    /// </summary>
    public string? Description { get; set; }
    
    // ========== Status & Type ==========
    
    /// <summary>
    /// Issue durumu
    /// </summary>
    public IssueStatus Status { get; set; } = IssueStatus.Open;
    
    /// <summary>
    /// Issue önceliği
    /// </summary>
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;
    
    /// <summary>
    /// Issue tipi (Bug, Feature, Task, Story, Epic)
    /// </summary>
    public IssueType Type { get; set; } = IssueType.Task;
    
    // ========== Relations ==========
    
    /// <summary>
    /// Bağlı olduğu proje
    /// </summary>
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    /// <summary>
    /// Parent issue (hiyerarşi için: Epic → Story → Task)
    /// </summary>
    public Guid? ParentIssueId { get; set; }
    public Issue? ParentIssue { get; set; }
    
    // ========== People ==========
    
    /// <summary>
    /// Issue'yu oluşturan kullanıcı (Identity Service'den)
    /// </summary>
    public string ReporterId { get; set; } = null!;
    
    /// <summary>
    /// Issue'ya atanan kullanıcı (Identity Service'den)
    /// </summary>
    public string? AssigneeId { get; set; }
    
    // ========== Dates ==========
    
    /// <summary>
    /// Son teslim tarihi
    /// </summary>
    public DateTime? DueDate { get; set; }
    
    /// <summary>
    /// Tahmini süre (saat)
    /// </summary>
    public decimal? EstimatedHours { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    /// <summary>
    /// Kapanış tarihi (Status = Closed olduğunda set edilir)
    /// </summary>
    public DateTime? ClosedAtUtc { get; set; }
    
    // ========== Concurrency ==========
    
    /// <summary>
    /// Optimistic concurrency token
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
    
    // ========== Navigation ==========
    
    /// <summary>
    /// Alt issue'lar (bu issue bir Epic veya Story ise)
    /// </summary>
    public ICollection<Issue> ChildIssues { get; set; } = new List<Issue>();
}
