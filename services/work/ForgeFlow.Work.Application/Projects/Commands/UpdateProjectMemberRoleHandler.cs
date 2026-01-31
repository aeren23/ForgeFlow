using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Services;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Commands;

public class UpdateProjectMemberRoleHandler : IRequestHandler<UpdateProjectMemberRoleCommand, bool>
{
    private readonly IWorkDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectPermissionService _permissionService;

    public UpdateProjectMemberRoleHandler(
        IWorkDbContext context,
        ICurrentUserService currentUser,
        IProjectPermissionService permissionService)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<bool> Handle(UpdateProjectMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Key == request.ProjectKey.ToUpperInvariant(), cancellationToken);

        if (project == null) return false;

        // Yetki Kontrolü
        var currentUserId = _currentUser.UserId;
        var member = project.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // System Admin override
        var isSystemAdmin = _currentUser.IsInRole("Admin");
        var role = isSystemAdmin ? ProjectRole.Owner : (member?.Role ?? ProjectRole.Viewer);

        // Use CanManageMembers for basic permission check
        if (!_permissionService.CanManageMembers(role))
        {
            throw new UnauthorizedAccessException("You are not authorized to update member roles in this project.");
        }

        // Validate New Role Enum
        if (!Enum.TryParse<ProjectRole>(request.Role, true, out var newRoleEnum))
        {
            throw new ArgumentException($"Invalid role: {request.Role}");
        }

        // --- HIERARCHY VALIDATION (Security Fix) ---
        var targetMember = project.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (targetMember == null) throw new KeyNotFoundException("Member not found in project.");

        // 1. Cannot modify someone with a higher or equal role (Strict Hierarchy)
        // RANKING: Lower value = Higher Rank (Owner=1, Admin=2, Member=3)
        // Rule: Block if RequesterRank >= TargetRank (meaning I am lower or equal to them)
        if (role >= targetMember.Role && role != ProjectRole.Owner)
        {
            throw new UnauthorizedAccessException($"You cannot modify a member with the role {targetMember.Role}.");
        }

        // 2. Cannot promote someone to a role higher or equal to yourself
        // Rule: Block if NewRole <= RequesterRole (meaning new role is higher or equal to me)
        if (newRoleEnum <= role && role != ProjectRole.Owner)
        {
            throw new UnauthorizedAccessException($"You cannot assign the role {newRoleEnum} as it is equal to or higher than your own.");
        }

        // Domain Logic
        project.UpdateMemberRole(request.UserId, newRoleEnum);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
