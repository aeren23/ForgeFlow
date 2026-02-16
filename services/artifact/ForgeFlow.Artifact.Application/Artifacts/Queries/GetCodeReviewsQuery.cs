using MediatR;

namespace ForgeFlow.Artifact.Application.Artifacts.Queries;

/// <summary>
/// Bir issue'ya ait tüm CODE_REVIEW artifact revision'larını döndürür.
/// Frontend Reviews tab'ı bu endpoint'i kullanır.
/// </summary>
public record GetCodeReviewsQuery(string ProjectId, string IssueId)
    : IRequest<List<CodeReviewDto>>;

public record CodeReviewDto(
    Guid ArtifactId,
    int RevisionNo,
    string ContentJson,
    string? CorrelationId,
    string? Metadata,
    DateTime CreatedAtUtc
);
