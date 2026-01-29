using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.AiOrchestrator.Application.Commands;

/// <summary>
/// Handler for GenerateAiPlanCommand.
/// Orchestrates the AI plan generation flow.
/// </summary>
public class GenerateAiPlanCommandHandler : IRequestHandler<GenerateAiPlanCommand, GenerateAiPlanResult>
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly IContextProvider _contextProvider;
    private readonly ILogger<GenerateAiPlanCommandHandler> _logger;

    public GenerateAiPlanCommandHandler(
        IAiServiceFactory aiServiceFactory,
        IContextProvider contextProvider,
        ILogger<GenerateAiPlanCommandHandler> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _contextProvider = contextProvider;
        _logger = logger;
    }


    public async Task<GenerateAiPlanResult> Handle(GenerateAiPlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating AI plan for Issue={IssueKey}, Project={ProjectId}, Provider={Provider}",
            request.IssueKey, request.ProjectId, request.PreferredProvider?.ToString() ?? "Default");

        // 1. Gather context from Work Service
        var context = await _contextProvider.GetContextAsync(
            request.ProjectId, 
            request.IssueKey, 
            cancellationToken);

        // 2. Build prompts
        var (systemPrompt, userPrompt) = BuildPrompts(request, context);

        // 3. Parse preferred provider from string (Worker sends string, we parse to enum here)
        AiProviderType? preferredProvider = null;
        if (!string.IsNullOrEmpty(request.PreferredProvider))
        {
            if (Enum.TryParse<AiProviderType>(request.PreferredProvider, ignoreCase: true, out var parsedProvider))
            {
                preferredProvider = parsedProvider;
            }
            else
            {
                _logger.LogWarning("Invalid PreferredProvider value: {Value}. Using default.", request.PreferredProvider);
            }
        }

        // 4. Get the appropriate AI service
        var aiService = _aiServiceFactory.GetService(preferredProvider);

        _logger.LogInformation("Using AI provider: {Provider}, Model: {Model}", 
            aiService.ProviderType, aiService.ModelName);

        // 5. Generate content
        var aiRequest = new AiRequest
        {
            RequestId = request.RequestId,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            MaxTokens = 4096,
            Temperature = 0.7,
            PreferredProvider = preferredProvider
        };

        var response = await aiService.GenerateContentAsync(aiRequest, cancellationToken);

        // 5. Return result
        if (response.IsSuccess)
        {
            _logger.LogInformation(
                "AI plan generated successfully. Tokens: {Prompt}+{Completion}, Duration: {Duration}ms",
                response.PromptTokens, response.CompletionTokens, response.DurationMs);

            return GenerateAiPlanResult.Success(response);
        }
        else
        {
            _logger.LogWarning(
                "AI plan generation failed: {ErrorCode} - {ErrorMessage}",
                response.ErrorCode, response.ErrorMessage);

            return GenerateAiPlanResult.Failure(
                response.ErrorMessage ?? "Unknown error",
                response.ErrorCode ?? "UNKNOWN",
                response.Provider);
        }
    }

    private static (string SystemPrompt, string UserPrompt) BuildPrompts(
        GenerateAiPlanCommand request, 
        AiContext context)
    {
        var systemPrompt = $"""
            Sen deneyimli bir Software Architect'sin. ForgeFlow projesi için teknik planlar üretiyorsun.
            
            Kurallar:
            - Clean Architecture prensiplerini uygula
            - SOLID prensiplerini takip et
            - Kod örnekleri C# olmalı (.NET 8)
            - Çıktını JSON formatında ver
            - Mevcut kodu analiz et ve ona uygun değişiklikler öner
            
            Strictness: {request.Strictness}
            Bundle Type: {request.BundleType}
            """;

        var techStackInfo = context.Project.TechStack.Count > 0
            ? string.Join(", ", context.Project.TechStack)
            : "Not specified";

        // Build code context section (from GitHub when available)
        var codeContextSection = BuildCodeContextSection(context);

        var userPrompt = $"""
            ## Proje Bilgisi
            - **Proje:** {context.Project.Name} ({context.Project.Key})
            - **Açıklama:** {context.Project.Description ?? "Yok"}
            - **Tech Stack:** {techStackInfo}
            - **Repository:** {context.Project.RepositoryUrl ?? "Bağlı değil"}
            
            ## Issue Bilgisi
            - **Issue:** {context.Issue.Key} - {context.Issue.Title}
            - **Tip:** {context.Issue.Type ?? "Task"}
            - **Öncelik:** {context.Issue.Priority ?? "Normal"}
            - **Açıklama:** {context.Issue.Description ?? "Detay yok"}
            
            {codeContextSection}
            
            ## Görev
            Bu issue için detaylı bir implementation plan oluştur. Plan şunları içermeli:
            1. Yapılacak değişikliklerin listesi
            2. Dosya bazında değişiklikler
            3. Kod örnekleri
            4. Test senaryoları
            """;

        return (systemPrompt, userPrompt);
    }

    /// <summary>
    /// Builds the code context section from GitHub source files (when available)
    /// </summary>
    private static string BuildCodeContextSection(AiContext context)
    {
        // If no code context available (GitHub not connected yet)
        if (!context.HasCodeContext)
        {
            return """
                ## Kod Bağlamı
                ⚠️ GitHub entegrasyonu henüz yapılmadığı için kod bağlamı mevcut değil.
                Genel mimari prensiplere göre plan oluştur.
                """;
        }

        // Build file tree if available
        var fileTreeSection = !string.IsNullOrEmpty(context.FileTreeStructure)
            ? $"""
                ### Proje Yapısı
                ```
                {context.FileTreeStructure}
                ```
                """
            : "";

        // Build source code snippets
        var sourceCodeSection = new System.Text.StringBuilder();
        sourceCodeSection.AppendLine("### Mevcut Kod Dosyaları");
        
        foreach (var file in context.SourceFiles.Take(10)) // Limit to 10 files for token efficiency
        {
            var language = file.Language ?? DetectLanguage(file.Path);
            sourceCodeSection.AppendLine($"""
                
                #### `{file.Path}`
                ```{language}
                {TruncateContent(file.Content, 500)}
                ```
                """);
        }

        if (context.SourceFiles.Count > 10)
        {
            sourceCodeSection.AppendLine($"\n*...ve {context.SourceFiles.Count - 10} dosya daha*");
        }

        return $"""
            ## Kod Bağlamı (GitHub'dan)
            {fileTreeSection}
            {sourceCodeSection}
            """;
    }

    private static string DetectLanguage(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".ts" => "typescript",
            ".tsx" => "tsx",
            ".js" => "javascript",
            ".jsx" => "jsx",
            ".json" => "json",
            ".yaml" or ".yml" => "yaml",
            ".md" => "markdown",
            ".sql" => "sql",
            _ => ""
        };
    }

    private static string TruncateContent(string content, int maxLines)
    {
        var lines = content.Split('\n');
        if (lines.Length <= maxLines)
            return content;

        var truncated = string.Join("\n", lines.Take(maxLines));
        return truncated + $"\n// ... ({lines.Length - maxLines} satır daha)";
    }
}
