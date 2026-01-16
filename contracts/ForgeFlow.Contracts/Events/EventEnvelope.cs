namespace ForgeFlow.Contracts.Events;

/// <summary>
/// Tüm event'leri saran zarf. CorrelationId ile idempotency, UserId ile audit sağlanır.
/// </summary>
public record EventEnvelope<T>(
    Guid EventId,
    DateTime OccurredAtUtc,
    string CorrelationId,
    string UserId,         // İşlemi başlatan kullanıcı
    string? CausationId,
    T Data
);

