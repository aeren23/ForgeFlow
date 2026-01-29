namespace ForgeFlow.Work.Domain.Entities;

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
    public string Role { get; private set; } = "Member";

    public DateTime JoinedAtUtc { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Project Project { get; private set; } = null!;

    private ProjectMember() { } // EF Core

    public ProjectMember(Guid projectId, string userId, string role)
    {
        ProjectId = projectId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = DateTime.UtcNow;
    }
}
