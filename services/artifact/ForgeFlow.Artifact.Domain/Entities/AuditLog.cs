namespace ForgeFlow.Artifact.Domain.Entities;

/// <summary>
/// Audit log kaydı - "Kim, ne zaman, ne yaptı?" sorusuna cevap verir.
/// Bu tablo sistemin "kara kutusu"dur.
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// İşlem yapılan entity adı (örn: "Artifact", "ArtifactRevision")
    /// </summary>
    public string EntityName { get; private set; } = default!;

    /// <summary>
    /// Entity'nin ID'si (Artifact GUID veya Revision GUID)
    /// </summary>
    public string EntityId { get; private set; } = default!;

    /// <summary>
    /// Yapılan işlem (Create, Update, Delete)
    /// </summary>
    public string Action { get; private set; } = default!;

    /// <summary>
    /// İşlemi yapan kullanıcı
    /// </summary>
    public string UserId { get; private set; } = default!;

    /// <summary>
    /// İşlemi tetikleyen event'in CorrelationId'si (distributed tracing için)
    /// </summary>
    public string CorrelationId { get; private set; } = default!;

    /// <summary>
    /// İşlem zamanı
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Opsiyonel: Değişiklik detayları (JSON olarak eski/yeni farkı)
    /// </summary>
    public string? Changes { get; private set; }

    private AuditLog() { } // EF Core için private constructor

    public AuditLog(string entityName, string entityId, string action, string userId, string correlationId, string? changes = null)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        UserId = userId;
        CorrelationId = correlationId;
        Changes = changes;
    }
}
