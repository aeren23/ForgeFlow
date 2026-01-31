namespace ForgeFlow.Work.Domain.Entities;

using ForgeFlow.Work.Domain.Enums;

/// <summary>
/// A user assigned to a project with a specific role.
/// </summary>
public class ProjectMember
{
    public Guid ProjectId { get; private set; }
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// Role within the project: "Owner", "Admin", "Member", "Viewer"
    /// </summary>
    public ProjectRole Role { get; private set; } = ProjectRole.Member;

    public DateTime JoinedAtUtc { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Project Project { get; private set; } = null!;

    private ProjectMember() { } // EF Core

    public ProjectMember(Guid projectId, string userId, ProjectRole role)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = DateTime.UtcNow;
    }
    internal void UpdateRole(ProjectRole newRole)
    {
        Role = newRole;
    }
}
