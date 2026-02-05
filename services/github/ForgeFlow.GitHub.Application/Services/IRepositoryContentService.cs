namespace ForgeFlow.GitHub.Application.Services;

/// <summary>
/// Repository içerik servisi - AI Orchestrator için tree ve file contents
/// </summary>
public interface IRepositoryContentService
{
    /// <summary>
    /// Repository ağacını döndürür (dosya/klasör listesi)
    /// </summary>
    Task<RepositoryTreeDto> GetTreeAsync(Guid projectId, string? path = null);

    /// <summary>
    /// Belirtilen dosyaların içeriklerini döndürür
    /// </summary>
    Task<Dictionary<string, string>> GetFilesAsync(Guid projectId, string[] filePaths);
}

/// <summary>
/// Repository ağacı DTO
/// </summary>
public record RepositoryTreeDto(
    string Path,
    TreeItemDto[] Items
);

/// <summary>
/// Ağaç içindeki dosya/klasör bilgisi
/// </summary>
public record TreeItemDto(
    string Path,
    string Name,
    string Type,  // "file" | "dir"
    long? Size
);
