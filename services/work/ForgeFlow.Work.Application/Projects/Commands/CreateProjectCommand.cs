using ForgeFlow.Work.Domain.Enums;
using MediatR;

namespace ForgeFlow.Work.Application.Projects.Commands;

/// <summary>
/// Yeni proje oluşturma komutu
/// </summary>
public record CreateProjectCommand(
    string Key,
    string Name,
    string? Description,
    string? RepositoryUrl,
    RepositoryProvider? RepositoryProvider,
    string[] TechStack,
    ProjectType ProjectType,
    string CreatorId
) : IRequest<CreateProjectResult>;

public record CreateProjectResult(
    Guid Id,
    string Key,
    string Name
);
