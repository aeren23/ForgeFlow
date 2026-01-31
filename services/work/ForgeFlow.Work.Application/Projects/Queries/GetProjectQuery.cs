using ForgeFlow.Work.Domain.Enums;
using MediatR;

namespace ForgeFlow.Work.Application.Projects.Queries;

/// <summary>
/// Tek proje getirme sorgusu
/// </summary>
public record GetProjectQuery(string Key) : IRequest<ProjectDto?>;

/// <summary>
/// Proje DTO
/// </summary>
public record ProjectDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    string? RepositoryUrl,
    RepositoryProvider? RepositoryProvider,
    string DefaultBranch,
    string[] TechStack,
    ProjectType ProjectType,
    string CreatorId,
    int IssueCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? CurrentUserRole, // The role of the requesting user in this project
    ICollection<ProjectMemberDto> Members
);
public record ProjectMemberDto(
    string UserId,
    string Role,
    DateTime JoinedAtUtc
);
