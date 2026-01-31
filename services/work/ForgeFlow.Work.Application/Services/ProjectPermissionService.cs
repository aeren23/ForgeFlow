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
        // Owner/Admin can assign anyone
        if (role == ProjectRole.Owner || role == ProjectRole.Admin || role == ProjectRole.TechLead)
        {
            return true;
        }

        // Member: Can ONLY assign themselves ("Self-Assignment")
        if (role == ProjectRole.Member)
        {
            return targetUserId == currentUserId;
        }

        // Viewer: Cannot assign
        return false;
    }

    public bool CanTransitionIssue(ProjectRole role, IssueStatus currentStatus, IssueStatus newStatus)
    {
        // Viewer: No transitions
        if (role == ProjectRole.Viewer) return false;

        // Owner/Admin: Can do anything
        if (role == ProjectRole.Owner || role == ProjectRole.Admin || role == ProjectRole.TechLead)
        {
            return true;
        }

        // Member Logic
        if (role == ProjectRole.Member)
        {
            // Cannot mark as Done (Must go to InReview)
            if (newStatus == IssueStatus.Done)
            {
                return false;
            }

            // Cannot Close
            if (newStatus == IssueStatus.Closed)
            {
                return false;
            }

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
