using Microsoft.AspNetCore.Identity;

namespace ForgeFlow.Identity.Domain.Entities;

/// <summary>
/// Uygulama rolü - IdentityRole'dan türetilmiş.
/// Ek alanlarla genişletilmiş: Description, CreatedAt, IsSystem
/// </summary>
public class ApplicationRole : IdentityRole
{
    /// <summary>
    /// Rolün açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Rol oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Sistem rolü mü? (silinemez/değiştirilemez)
    /// Admin, TeamLead, Developer gibi temel roller sistem rolüdür
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Varsayılan constructor
    /// </summary>
    public ApplicationRole() : base() { }

    /// <summary>
    /// İsimli constructor
    /// </summary>
    public ApplicationRole(string roleName) : base(roleName) { }

    /// <summary>
    /// Tam constructor
    /// </summary>
    public ApplicationRole(string roleName, string? description, bool isSystem = false) : base(roleName)
    {
        Description = description;
        IsSystem = isSystem;
    }
}

/// <summary>
/// Varsayılan sistem rolleri
/// </summary>
public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string TeamLead = "TeamLead";
    public const string Developer = "Developer";
    public const string Viewer = "Viewer";

    public static readonly List<(string Name, string Description)> All = new()
    {
        (Admin, "Sistem yöneticisi - tüm yetkilere sahip"),
        (TeamLead, "Takım lideri - proje ve takım yönetimi"),
        (Developer, "Geliştirici - issue ve artifact yönetimi"),
        (Viewer, "İzleyici - sadece okuma yetkisi")
    };
}
