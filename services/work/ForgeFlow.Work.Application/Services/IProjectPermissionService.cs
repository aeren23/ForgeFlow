using ForgeFlow.Work.Domain.Entities;
using ForgeFlow.Work.Domain.Enums;

namespace ForgeFlow.Work.Application.Services;

public interface IProjectPermissionService
{
    // Issue Permissions
    bool CanCreateIssue(ProjectRole role);
    bool CanEditIssue(ProjectRole role, string creatorId, string currentUserId);
    bool CanDeleteIssue(ProjectRole role, string creatorId, string currentUserId);
    bool CanAssignUser(ProjectRole role, string targetUserId, string currentUserId);
    bool CanTransitionIssue(ProjectRole role, IssueStatus currentStatus, IssueStatus newStatus, string? issueAssigneeId, string currentUserId);

    // Project Permissions
    bool CanUpdateProjectConfig(ProjectRole role);
    bool CanDeleteProject(ProjectRole role);
    bool CanManageMembers(ProjectRole role);
}
