namespace ForgeFlow.Artifact.Application.Abstractions;

/// <summary>
/// Bu interface'i implement eden Command'lar otomatik olarak audit loglanır.
/// AuditBehavior<T> pipeline behavior tarafından yakalanır.
/// </summary>
public interface IAuditLoggable
{
    /// <summary>
    /// İşlem yapılan entity adı (örn: "Artifact", "ArtifactRevision")
    /// </summary>
    string EntityName { get; }

    /// <summary>
    /// Entity'nin ID'si (Artifact GUID veya key)
    /// </summary>
    string EntityId { get; }

    /// <summary>
    /// Yapılan işlem (Create, Update, Delete)
    /// </summary>
    string Action { get; }

    /// <summary>
    /// İşlemi yapan kullanıcı
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// İşlemi tetikleyen event'in CorrelationId'si
    /// </summary>
    string CorrelationId { get; }
}
