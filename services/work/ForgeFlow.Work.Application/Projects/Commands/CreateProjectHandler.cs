using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Domain.Entities;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Commands;

/// <summary>
/// Proje oluşturma handler
/// </summary>
public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, CreateProjectResult>
{
    private readonly IWorkDbContext _context;

    public CreateProjectHandler(IWorkDbContext context)
    {
        _context = context;
    }

    public async Task<CreateProjectResult> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        // Key'in unique olduğunu kontrol et
        var keyExists = await _context.Projects
            .AnyAsync(p => p.Key == request.Key.ToUpperInvariant(), cancellationToken);

        if (keyExists)
        {
            throw new InvalidOperationException($"Project key '{request.Key}' already exists");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Key = request.Key.ToUpperInvariant(),
            Name = request.Name,
            Description = request.Description,
            RepositoryUrl = request.RepositoryUrl,
            RepositoryProvider = request.RepositoryProvider,
            DefaultBranch = "main",
            TechStack = request.TechStack ?? [],
            ProjectType = request.ProjectType,
            CreatorId = request.CreatorId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true,
            NextIssueNumber = 1
        };

        // Add creator as "Owner"
        project.AddMember(request.CreatorId, ProjectRole.Owner);

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateProjectResult(project.Id, project.Key, project.Name);
    }
}
