using ForgeFlow.Work.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Commands;

/// <summary>
/// Proje güncelleme handler
/// </summary>
public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, UpdateProjectResult>
{
    private readonly IWorkDbContext _context;

    public UpdateProjectHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateProjectResult> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Key == request.Key.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Project '{request.Key}' not found");

        project.Name = request.Name;
        project.Description = request.Description;
        project.RepositoryUrl = request.RepositoryUrl;
        project.RepositoryProvider = request.RepositoryProvider;
        project.DefaultBranch = request.DefaultBranch;
        project.TechStack = request.TechStack ?? [];
        project.ProjectType = request.ProjectType;
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateProjectResult(project.Id, project.Key, project.Name);
    }
}
