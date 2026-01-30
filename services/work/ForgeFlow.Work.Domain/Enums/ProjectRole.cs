namespace ForgeFlow.Work.Domain.Enums;

public enum ProjectRole
{
    Owner = 1,
    Admin = 2, // TechLead
    Member = 3, // Developer
    Viewer = 4,
    TechLead = 5 // Deprecated, maps to Admin conceptually but kept for data compatibility
}
