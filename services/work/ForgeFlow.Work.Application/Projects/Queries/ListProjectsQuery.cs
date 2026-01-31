using MediatR;

namespace ForgeFlow.Work.Application.Projects.Queries;

/// <summary>
/// Proje listeleme sorgusu
/// </summary>
public record ListProjectsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = true
) : IRequest<ListProjectsResult>;

public record ListProjectsResult(
    IReadOnlyList<ProjectListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record ProjectListItemDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    string[] TechStack,
    int IssueCount,
    DateTime CreatedAtUtc,
    IReadOnlyList<ProjectMemberDto> Members
);


