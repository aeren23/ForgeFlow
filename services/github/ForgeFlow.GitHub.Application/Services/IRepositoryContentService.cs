namespace ForgeFlow.GitHub.Application.Services;

/// <summary>
/// Repository içerik servisi - AI Orchestrator için tree ve file contents
/// </summary>
public interface IRepositoryContentService
{
    /// <summary>
    /// Repository ağacını döndürür (dosya/klasör listesi - shallow)
    /// </summary>
    Task<RepositoryTreeDto> GetTreeAsync(Guid projectId, string? path = null);

    /// <summary>
    /// Repository'nin recursive dosya ağacını döndürür (Git Trees API)
    /// Binary dosyalar filtrelenir, max item limiti uygulanır
    /// </summary>
    Task<RepositoryTreeDto> GetRecursiveTreeAsync(Guid projectId, int maxItems = 500);

    /// <summary>
    /// Belirtilen dosyaların içeriklerini döndürür
    /// </summary>
    Task<Dictionary<string, string>> GetFilesAsync(Guid projectId, string[] filePaths);

    /// <summary>
    /// Repo'yu analiz eder: recursive tree + kritik dosya içerikleri + tech stack tespiti
    /// AI Orchestrator'ın Code Context Bridge ve Workflow Generation için kullandığı ana method
    /// </summary>
    Task<RepoAnalysisResult> AnalyzeRepoAsync(Guid projectId);
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

/// <summary>
/// Repo analiz sonucu — AI Orchestrator tarafından code context ve workflow generation için kullanılır
/// </summary>
public record RepoAnalysisResult(
    /// <summary>Recursive dosya ağacı path'leri (max 500 item, binary hariç)</summary>
    string[] TreePaths,
    /// <summary>Otomatik tespit edilen kritik dosya içerikleri (sln, package.json, Dockerfile vb.)</summary>
    Dictionary<string, string> CriticalFiles,
    /// <summary>Tespit edilen teknoloji yığını</summary>
    string[] DetectedTechStack,
    /// <summary>.github/workflows/ altındaki mevcut workflow dosyaları</summary>
    string[] ExistingWorkflows
);
