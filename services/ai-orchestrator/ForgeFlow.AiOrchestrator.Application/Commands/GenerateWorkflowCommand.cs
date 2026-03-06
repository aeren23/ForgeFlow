using MediatR;

namespace ForgeFlow.AiOrchestrator.Application.Commands;

/// <summary>
/// AI Workflow Generation komutu — Proje repo yapısını analiz ederek
/// GitHub Actions CI/CD pipeline YAML dosyası üretir
/// </summary>
public class GenerateWorkflowCommand : IRequest<GenerateWorkflowResult>
{
    /// <summary>İzleme amaçlı benzersiz request ID</summary>
    public Guid RequestId { get; init; } = Guid.NewGuid();

    /// <summary>Proje ID</summary>
    public Guid ProjectId { get; init; }

    /// <summary>İstekte bulunan kullanıcı ID</summary>
    public string UserId { get; init; } = "";

    /// <summary>Tercih edilen AI provider (null = default)</summary>
    public string? PreferredProvider { get; init; }

    /// <summary>Repo analiz sonuçları — tree paths</summary>
    public string[] TreePaths { get; init; } = [];

    /// <summary>Kritik dosya içerikleri</summary>
    public Dictionary<string, string> CriticalFiles { get; init; } = new();

    /// <summary>Tespit edilen tech stack</summary>
    public string[] DetectedTechStack { get; init; } = [];

    /// <summary>Mevcut workflow dosyaları</summary>
    public string[] ExistingWorkflows { get; init; } = [];
}

/// <summary>
/// Workflow generation sonucu
/// </summary>
public class GenerateWorkflowResult
{
    public bool IsSuccess { get; init; }
    public string? WorkflowYaml { get; init; }
    public string? WorkflowFileName { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public string? UsedProvider { get; init; }
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public long DurationMs { get; init; }

    public static GenerateWorkflowResult Success(
        string workflowYaml, string fileName, string provider,
        int promptTokens, int completionTokens, long durationMs) => new()
    {
        IsSuccess = true,
        WorkflowYaml = workflowYaml,
        WorkflowFileName = fileName,
        UsedProvider = provider,
        PromptTokens = promptTokens,
        CompletionTokens = completionTokens,
        DurationMs = durationMs
    };

    public static GenerateWorkflowResult Failure(string error, string errorCode) => new()
    {
        IsSuccess = false,
        ErrorMessage = error,
        ErrorCode = errorCode
    };
}
