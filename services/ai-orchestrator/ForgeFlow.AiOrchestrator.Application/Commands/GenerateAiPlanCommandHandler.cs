using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Domain.Models;
using ForgeFlow.Contracts.Events;
using MassTransit;
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
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<GenerateAiPlanCommandHandler> _logger;

    public GenerateAiPlanCommandHandler(
        IAiServiceFactory aiServiceFactory,
        IContextProvider contextProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<GenerateAiPlanCommandHandler> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _contextProvider = contextProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }


    public async Task<GenerateAiPlanResult> Handle(GenerateAiPlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating AI plan for Issue={IssueKey}, Project={ProjectId}, Provider={Provider}",
            request.IssueKey, request.ProjectId, request.PreferredProvider?.ToString() ?? "Default");

        // Progress: Starting
        await PublishProgressAsync(request, "AI plan üretimi başlatılıyor...", 5);

        // 1. Gather context from Work Service
        await PublishProgressAsync(request, "Proje ve issue bilgileri alınıyor...", 15);
        var context = await _contextProvider.GetContextAsync(
            request.ProjectId,
            request.IssueKey,
            cancellationToken);

        // Progress: Context gathered
        await PublishProgressAsync(request, "Context toplandı, prompt hazırlanıyor...", 30);

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

        // Progress: AI call starting
        await PublishProgressAsync(request, $"AI modeline ({aiService.ModelName}) gönderiliyor...", 50);

        // 5. Generate content
        var aiRequest = new AiRequest
        {
            RequestId = request.RequestId,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            MaxTokens = 8192, // Increased from 4096 to prevent JSON truncation
            Temperature = 0.7,
            PreferredProvider = preferredProvider
        };

        var response = await aiService.GenerateContentAsync(aiRequest, cancellationToken);

        // Progress: Response received
        await PublishProgressAsync(request, "AI yanıtı alındı, işleniyor...", 85);

        // 6. Return result
        if (response.IsSuccess)
        {
            _logger.LogInformation(
                "AI plan generated successfully. Tokens: {Prompt}+{Completion}, Duration: {Duration}ms",
                response.PromptTokens, response.CompletionTokens, response.DurationMs);

            // Progress: Orchestrator Complete (Step 1/2)
            await PublishProgressAsync(request, "AI plan başarıyla oluşturuldu! Issue'lar oluşturulacak...", 90);

            return GenerateAiPlanResult.Success(response);
        }
        else
        {
            _logger.LogWarning(
                "AI plan generation failed: {ErrorCode} - {ErrorMessage}",
                response.ErrorCode, response.ErrorMessage);

            // Progress: Failed
            await PublishProgressAsync(request, $"Hata: {response.ErrorMessage}", 100);

            return GenerateAiPlanResult.Failure(
                response.ErrorMessage ?? "Unknown error",
                response.ErrorCode ?? "UNKNOWN",
                response.Provider);
        }
    }

    /// <summary>
    /// Publishes AI processing progress event for real-time updates via SignalR
    /// </summary>
    private async Task PublishProgressAsync(GenerateAiPlanCommand request, string message, int progressPercentage)
    {
        try
        {
            var progressEvent = new AiProcessingProgress(
                RequestId: request.RequestId,
                ProjectId: request.ProjectId,
                UserId: request.UserId,
                Message: message,
                ProgressPercentage: progressPercentage,
                Timestamp: DateTime.UtcNow
            );

            await _publishEndpoint.Publish(progressEvent);

            _logger.LogDebug("Published progress: {Progress}% - {Message}", progressPercentage, message);
        }
        catch (Exception ex)
        {
            // Don't fail the main flow if progress publishing fails
            _logger.LogWarning(ex, "Failed to publish progress event: {Message}", message);
        }
    }

    private static (string SystemPrompt, string UserPrompt) BuildPrompts(
    GenerateAiPlanCommand request,
    AiContext context)
    {
        // 1. TechStack'i düzgün bir string haline getirelim
        var techStack = context.Project.TechStack is { Count: > 0 }
            ? string.Join(", ", context.Project.TechStack)
            : "Genel modern yazılım dilleri ve mimarileri";

        // 2. SYSTEM PROMPT: AI'ya kimliğini ve projenin "ruhunu" veriyoruz
        // Sadece TechStack ve Issue bilgilerine odaklanmasını sağlıyoruz.
        var systemPrompt = $"""
        Sen bir Senior Software Engineer'sın. ForgeFlow platformu üzerinden teknik planlar üretiyorsun.
        
        ## Mimari Prensiplerin:
        - Projenin ana teknolojileri: {techStack}
        - Eğer mevcutsa, projenin klasör yapısını ve kod stilini analiz et.
        - Çözümlerin mutlaka projenin kullandığı teknoloji yığınına ({techStack}) uygun olmalı.
        - YANITIN SADECE VE SADECE GEÇERLİ RAW JSON OLMALI.
        - Asla Markdown blokları (```json ... ```) kullanma.
        - JSON içindeki tüm stringlerdeki satır sonlarını escape et (\n).
        - Gereksiz açıklamalardan kaçın, doğrudan teknik implementasyona ve dosya bazlı değişikliklere odaklan.
        """;

        // 3. CODE CONTEXT: GitHub'dan gelen dosyaları (varsa) buraya ekleyeceğiz
        var codeContextSection = BuildCodeContextSection(context);

        // 4. USER PROMPT: Elimizdeki modelleri (ProjectContextDto & IssueContextDto) kullanıyoruz
        var userPrompt = $"""
        Aşağıdaki issue için detaylı bir geliştirme planı üret:

        ### 📋 PROJE BİLGİSİ
        - İsim: {context.Project.Name} ({context.Project.Key})
        - Açıklama: {context.Project.Description ?? "Belirtilmemiş"}
        - Teknoloji Yığını: {techStack}

        ### 🎯 HEDEF (ISSUE)
        - Kimlik: {context.Issue.Key} - {context.Issue.Title}
        - Tanım: {context.Issue.Description ?? "Detaylı açıklama yok."}
        - Tip/Öncelik: {context.Issue.Type} / {context.Issue.Priority}

        {codeContextSection}

        ### 📝 BEKLENTİLER (JSON FORMATI)
        - summary: İşin genel teknik özeti (Max 2 cümle)
        - implementation_plan: 
            - summary: Implementasyonun kısa özeti
            - list_of_changes: Yapılacak EN ÖNEMLİ 5-7 maddenin listesi.
                - title: Task başlığı (Max 50 karakter)
                - description: İşin teknik özeti (Max 1 cümle, çok kısa)
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
