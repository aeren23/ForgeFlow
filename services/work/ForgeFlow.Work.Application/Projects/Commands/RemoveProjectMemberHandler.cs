using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Services;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Projects.Commands;

public class RemoveProjectMemberHandler : IRequestHandler<RemoveProjectMemberCommand, bool>
{
    private readonly IWorkDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectPermissionService _permissionService;

    public RemoveProjectMemberHandler(
        IWorkDbContext context,
        ICurrentUserService currentUser,
        IProjectPermissionService permissionService)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<bool> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Key == request.ProjectKey.ToUpperInvariant(), cancellationToken);

        if (project == null) return false;

        // Current User Logic
        var currentUserId = _currentUser.UserId;
        var currentUserMember = project.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // System Admin override
        var isSystemAdmin = _currentUser.IsInRole("Admin");
        var role = isSystemAdmin ? ProjectRole.Owner : (currentUserMember?.Role ?? ProjectRole.Viewer);

        // Self-Removal (Leaving the project)
        if (request.TargetUserId == currentUserId)
        {
            // Owner cannot leave if they are the only owner (Basic check, maybe separate logic?)
            // For now, allow leaving.
            project.RemoveMember(currentUserId);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // --- PERMISSION & HIERARCHY CHECK ---

        // 1. Basic Permission
        if (!_permissionService.CanManageMembers(role))
        {
            throw new UnauthorizedAccessException("You are not authorized to remove members from this project.");
        }

        var targetMember = project.Members.FirstOrDefault(m => m.UserId == request.TargetUserId);
        if (targetMember == null) return false; // Already gone?

        // 2. Hierarchy Check
        // Cannot remove someone with Higher or Equal Rank.
        // RANKING: Lower Value = Higher Rank (Owner=1, Admin=2, Member=3)
        // Rule: Block if MyRole >= TargetRole.
        // Exception: Owner(1) can remove other Owner(1)? Usually no, only transfer.
        // But if Owner(1) wants to kick another Owner(1), maybe? 
        // Let's stick to strict: Owner is king. If multiple owners, they are peers. Peers cannot kick peers potentially.
        // But for safety:
        if (role >= targetMember.Role && !isSystemAdmin)
        {
            // If I am Owner(1) and Target is Owner(1) -> 1 >= 1 -> Blocked.
            // If I am Admin(2) and Target is Member(3) -> 2 >= 3 -> False (Allowed).
            // If I am Admin(2) and Target is Admin(2) -> 2 >= 2 -> Blocked.
            throw new UnauthorizedAccessException($"You cannot remove a member with the role {targetMember.Role} (Equal or Higher Rank).");
        }

        // Domain Logic
        project.RemoveMember(request.TargetUserId);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
