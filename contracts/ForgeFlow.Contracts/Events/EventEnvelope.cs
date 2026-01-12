namespace ForgeFlow.Contracts.Events;

public record EventEnvelope<T>(
    Guid EventId,
    DateTime OccurredAtUtc,
    string CorrelationId,
    string? CausationId,
    T Data
);
