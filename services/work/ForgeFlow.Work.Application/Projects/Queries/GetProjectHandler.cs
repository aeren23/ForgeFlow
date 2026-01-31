using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Queries;

/// <summary>
/// Proje getirme handler
/// </summary>
public class GetProjectHandler : IRequestHandler<GetProjectQuery, ProjectDto?>
{
    private readonly IWorkDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectHandler(IWorkDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectDto?> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        Guid? projectId = null;
        if (Guid.TryParse(request.Key, out var parsedId))
        {
            projectId = parsedId;
        }

        var project = await _context.Projects
            .Include(p => p.Issues)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p =>
                (projectId.HasValue && p.Id == projectId.Value) ||
                p.Key == request.Key.ToUpperInvariant(), cancellationToken);

        if (project == null)
            return null;

        var currentUserId = _currentUserService.UserId;
        var currentUserMember = project.Members.FirstOrDefault(m => m.UserId == currentUserId);
        var currentUserRole = currentUserMember?.Role.ToString();

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
            project.UpdatedAtUtc,
            currentUserRole,
            project.Members.Select(m => new ProjectMemberDto(m.UserId, m.Role.ToString(), m.JoinedAtUtc)).ToList()
        );
    }
}
