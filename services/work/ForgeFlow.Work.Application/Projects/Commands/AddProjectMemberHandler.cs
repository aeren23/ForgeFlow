using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Services;
using ForgeFlow.Work.Domain.Entities;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Commands;

public class AddProjectMemberHandler : IRequestHandler<AddProjectMemberCommand, bool>
{
    private readonly IWorkDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectPermissionService _permissionService;

    public AddProjectMemberHandler(IWorkDbContext context, ICurrentUserService currentUser, IProjectPermissionService permissionService)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<bool> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Key == request.ProjectKey, cancellationToken);

        if (project == null) return false;

        // Yetki Hizmetini Kullan
        var currentUserId = _currentUser.UserId;
        var member = project.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // System Admin override
        var isSystemAdmin = _currentUser.IsInRole("Admin");
        var role = isSystemAdmin ? ProjectRole.Owner : (member?.Role ?? ProjectRole.Viewer);

        if (!_permissionService.CanManageMembers(role))
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
