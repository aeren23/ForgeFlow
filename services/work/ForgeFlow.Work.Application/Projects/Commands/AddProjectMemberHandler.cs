using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Domain.Entities;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Commands;

public class AddProjectMemberHandler : IRequestHandler<AddProjectMemberCommand, bool>
{
    private readonly IWorkDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddProjectMemberHandler(IWorkDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Key == request.ProjectKey, cancellationToken);

        if (project == null) return false;

        // AUTHORIZATION: Only Admin, Project Creator, or Project Owner/Admin can add members
        var currentUserId = _currentUser.UserId;
        var isAdmin = _currentUser.IsInRole("Admin");
        var isCreator = project.CreatorId == currentUserId;
        var isProjectAdmin = project.Members.Any(m => m.UserId == currentUserId && (m.Role == ProjectRole.Owner || m.Role == ProjectRole.Admin));

        if (!isAdmin && !isCreator && !isProjectAdmin)
        {
            throw new UnauthorizedAccessException("You are not authorized to add members to this project.");
        }

        if (!Enum.TryParse<ProjectRole>(request.Role, true, out var roleEnum))
        {
            throw new ArgumentException($"Invalid role: {request.Role}");
        }

        project.AddMember(request.UserId, roleEnum);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
