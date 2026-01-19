using ForgeFlow.Work.Domain.Enums;
using MediatR;

namespace ForgeFlow.Work.Application.Projects.Commands;

/// <summary>
/// Proje güncelleme komutu
/// </summary>
public record UpdateProjectCommand(
    string Key,
    string Name,
    string? Description,
    string? RepositoryUrl,
    RepositoryProvider? RepositoryProvider,
    string DefaultBranch,
    string[] TechStack,
    ProjectType ProjectType
) : IRequest<UpdateProjectResult>;

public record UpdateProjectResult(
    Guid Id,
    string Key,
    string Name
);
