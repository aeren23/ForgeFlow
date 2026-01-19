using ForgeFlow.Work.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Queries;

/// <summary>
/// Proje getirme handler
/// </summary>
public class GetProjectHandler : IRequestHandler<GetProjectQuery, ProjectDto?>
{
    private readonly IWorkDbContext _context;

    public GetProjectHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectDto?> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Issues)
            .FirstOrDefaultAsync(p => p.Key == request.Key.ToUpperInvariant(), cancellationToken);

        if (project == null)
            return null;

        return new ProjectDto(
            project.Id,
            project.Key,
            project.Name,
            project.Description,
            project.RepositoryUrl,
            project.RepositoryProvider,
            project.DefaultBranch,
            project.TechStack,
            project.ProjectType,
            project.CreatorId,
            project.Issues.Count,
            project.CreatedAtUtc,
            project.UpdatedAtUtc
        );
    }
}
