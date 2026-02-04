using ForgeFlow.Work.Domain.Entities;
using ForgeFlow.Work.Domain.Enums;

namespace ForgeFlow.Work.Application.Services;

public class ProjectPermissionService : IProjectPermissionService
{
    // --- Issue Permissions ---

    public bool CanCreateIssue(ProjectRole role)
    {
        // Viewers cannot create issues
        return role != ProjectRole.Viewer;
    }

    public bool CanEditIssue(ProjectRole role, string creatorId, string currentUserId)
    {
        // Owner/Admin/TechLead can any
        if (role == ProjectRole.Owner || role == ProjectRole.Admin || role == ProjectRole.TechLead)
        {
            return true;
        }

        // Member: Only their own issues (Matrix: "Edit Issue Details: Member ✅ (Own)")
        if (role == ProjectRole.Member)
        {
            return creatorId == currentUserId;
        }

        // Viewer: No
        return false;
    }

    public bool CanDeleteIssue(ProjectRole role, string creatorId, string currentUserId)
    {
        // Owner/Admin can delete any issue
        if (role == ProjectRole.Owner || role == ProjectRole.Admin || role == ProjectRole.TechLead)
        {
            return true;
        }

        // Member can ONLY delete their own issues? 
        // Matrix said: Member CANNOT delete issues.
        // Let's stick to the matrix: "Delete Issue: Owner/Admin ✅, Member ❌"
        return false;
    }

    public bool CanAssignUser(ProjectRole role, string targetUserId, string currentUserId)
    {
        // ONLY Owner/Admin/TechLead can assign users to issues
        // Members cannot assign anyone (including themselves)
        if (role is ProjectRole.Owner or ProjectRole.Admin or ProjectRole.TechLead)
        {
            return true;
        }

        // Member and Viewer: Cannot assign
        return false;
    }

    public bool CanTransitionIssue(ProjectRole role, IssueStatus currentStatus, IssueStatus newStatus, string? issueAssigneeId, string currentUserId)
    {
        // Viewer: No transitions allowed
        if (role == ProjectRole.Viewer) return false;

        // Owner/Admin/TechLead: Can do anything
        if (role is ProjectRole.Owner or ProjectRole.Admin or ProjectRole.TechLead)
        {
            return true;
        }

        // === MEMBER RULES ===
        if (role == ProjectRole.Member)
        {
            // 1. Cannot move to Done or Closed
            if (newStatus is IssueStatus.Done or IssueStatus.Closed)
            {
                return false;
            }

            // 2. Cannot move from Open (must be assigned first via AssignIssue)
            if (currentStatus == IssueStatus.Open)
            {
                return false;
            }

            // 3. Can only move issues assigned to themselves
            if (issueAssigneeId != currentUserId)
            {
                return false;
            }

            // 4. InProgress → InReview: OK
            // 5. InReview → InProgress: OK (revision)
            return true;
        }

        return false;
    }

    // --- Project Permissions ---

    public bool CanUpdateProjectConfig(ProjectRole role)
    {
        return role == ProjectRole.Owner || role == ProjectRole.Admin || role == ProjectRole.TechLead;
    }

    public bool CanDeleteProject(ProjectRole role)
    {
        // ONLY Owner can delete project
        return role == ProjectRole.Owner;
    }

    public bool CanManageMembers(ProjectRole role)
    {
        return role == ProjectRole.Owner || role == ProjectRole.Admin || role == ProjectRole.TechLead;
    }
}
