using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Domain.Models;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

        var logEntries = new List<string>();

        // Progress: Starting
        logEntries.Add("[START] AI plan üretimi başlatılıyor...");
        await PublishProgressAsync(request, "AI plan üretimi başlatılıyor...", 5, logEntries);

        // 1. Gather context from Work Service + GitHub Service (Code Context Bridge)
        logEntries.Add("[CONTEXT] Proje, issue ve kod bağlamı alınıyor...");
        await PublishProgressAsync(request, "Proje, issue ve kod bağlamı alınıyor...", 10, logEntries);

        var context = await _contextProvider.GetContextAsync(
            request.ProjectId,
            request.IssueKey,
            cancellationToken);

        // Log code context status
        if (context.HasCodeContext)
        {
            logEntries.Add($"[GITHUB] ✅ Kod bağlamı yüklendi: {context.SourceFiles.Count} dosya, dosya ağacı mevcut");
            await PublishProgressAsync(request, $"Kod bağlamı yüklendi: {context.SourceFiles.Count} kritik dosya", 20, logEntries);
        }
        else
        {
            logEntries.Add("[GITHUB] ⚠️ GitHub bağlantısı yok, genel mimari ile devam ediliyor");
            await PublishProgressAsync(request, "GitHub bağlantısı yok, genel mimari ile devam ediliyor", 20, logEntries);
        }

        // 2. Parse preferred provider
        AiProviderType? preferredProvider = null;
        if (!string.IsNullOrEmpty(request.PreferredProvider))
        {
            if (Enum.TryParse<AiProviderType>(request.PreferredProvider, ignoreCase: true, out var parsedProvider))
                preferredProvider = parsedProvider;
            else
                _logger.LogWarning("Invalid PreferredProvider value: {Value}. Using default.", request.PreferredProvider);
        }

        var aiService = _aiServiceFactory.GetService(preferredProvider);
        logEntries.Add($"[AI] Provider: {aiService.ProviderType}, Model: {aiService.ModelName}");

        // ===== MULTI-TURN AI LOOP =====
        // Phase 1: Eğer code context varsa, AI'a dosya ağacını gönder ve hangi dosyaları okumak istediğini sor
        if (context.HasCodeContext && !string.IsNullOrEmpty(context.FileTreeStructure))
        {
            logEntries.Add("[PHASE-1] AI'a dosya ağacı gönderiliyor, okunacak dosyalar sorulacak...");
            await PublishProgressAsync(request, "AI dosya ağacını analiz ediyor...", 30, logEntries);

            var discoveryResponse = await ExecuteFileDiscoveryPhase(
                aiService, request, context, cancellationToken);

            if (discoveryResponse != null && discoveryResponse.Count > 0)
            {
                logEntries.Add($"[PHASE-1] AI {discoveryResponse.Count} dosya okumak istiyor: {string.Join(", ", discoveryResponse.Take(5))}");
                await PublishProgressAsync(request,
                    $"AI {discoveryResponse.Count} dosya okumak istiyor...", 40,
                    logEntries, requestedFiles: discoveryResponse);

                // Phase 2: İstenen dosyaları GitHub Service'den çek
                logEntries.Add("[PHASE-2] İstenen dosyalar GitHub'dan çekiliyor...");
                await PublishProgressAsync(request, "İstenen dosyalar GitHub'dan çekiliyor...", 45, logEntries);

                var fetchedFiles = await _contextProvider.FetchRequestedFilesAsync(
                    request.ProjectId, discoveryResponse.ToArray(), cancellationToken);

                // Okunan dosyaları context'e ekle
                foreach (KeyValuePair<string, string> kvp in fetchedFiles)
                {
                    if (!context.SourceFiles.Any(f => f.Path == kvp.Key))
                    {
                        context.SourceFiles.Add(new CodeFileDto
                        {
                            Path = kvp.Key,
                            Content = kvp.Value,
                            Language = null
                        });
                    }
                }

                logEntries.Add($"[PHASE-2] ✅ {fetchedFiles.Count} dosya başarıyla okundu");
                await PublishProgressAsync(request, $"{fetchedFiles.Count} dosya okundu, plan üretiliyor...", 55, logEntries);
            }
            else
            {
                logEntries.Add("[PHASE-1] AI ek dosya istemedi, doğrudan plan üretecek");
            }
        }

        // ===== PLAN GENERATION =====
        logEntries.Add("[GENERATE] Prompt hazırlanıyor...");
        await PublishProgressAsync(request, "Prompt hazırlanıyor...", 60, logEntries);

        var (systemPrompt, userPrompt) = BuildPrompts(request, context);

        logEntries.Add($"[GENERATE] AI modeline ({aiService.ModelName}) gönderiliyor...");
        await PublishProgressAsync(request, $"AI modeline ({aiService.ModelName}) gönderiliyor...", 65, logEntries);

        var aiRequest = new AiRequest
        {
            RequestId = request.RequestId,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            MaxTokens = 8192,
            Temperature = 0.7,
            PreferredProvider = preferredProvider
        };

        var response = await aiService.GenerateContentAsync(aiRequest, cancellationToken);

        logEntries.Add($"[GENERATE] AI yanıtı alındı ({response.DurationMs}ms)");
        await PublishProgressAsync(request, "AI yanıtı alındı, işleniyor...", 85, logEntries);

        if (response.IsSuccess)
        {
            _logger.LogInformation(
                "AI plan generated successfully. Tokens: {Prompt}+{Completion}, Duration: {Duration}ms",
                response.PromptTokens, response.CompletionTokens, response.DurationMs);

            logEntries.Add($"[SUCCESS] ✅ Plan üretildi! Tokens: {response.PromptTokens}+{response.CompletionTokens}");
            await PublishProgressAsync(request, "AI plan başarıyla oluşturuldu! Issue'lar oluşturulacak...", 90, logEntries);

            return GenerateAiPlanResult.Success(response);
        }
        else
        {
            _logger.LogWarning(
                "AI plan generation failed: {ErrorCode} - {ErrorMessage}",
                response.ErrorCode, response.ErrorMessage);

            logEntries.Add($"[ERROR] ❌ Hata: {response.ErrorCode} - {response.ErrorMessage}");
            await PublishProgressAsync(request, $"Hata: {response.ErrorMessage}", 100, logEntries);

            return GenerateAiPlanResult.Failure(
                response.ErrorMessage ?? "Unknown error",
                response.ErrorCode ?? "UNKNOWN",
                response.Provider);
        }
    }

    /// <summary>
    /// Phase 1: AI'a dosya ağacını gösterir ve hangi dosyaları okumak istediğini sorar
    /// </summary>
    private async Task<List<string>?> ExecuteFileDiscoveryPhase(
        IAiService aiService,
        GenerateAiPlanCommand request,
        AiContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var discoveryPrompt = $$"""
            Aşağıdaki issue için bir geliştirme planı üreteceksin.
            Önce proje dosya yapısını incele ve planı üretmek için hangi dosyaları okuman gerektiğini belirle.
            
            ### Issue
            - {{context.Issue.Key}}: {{context.Issue.Title}}
            - Açıklama: {{context.Issue.Description ?? "Yok"}}
            - Tip: {{context.Issue.Type}}
            
            ### Proje Dosya Yapısı
            ```
            {{context.FileTreeStructure}}
            ```
            
            SADECE JSON formatında yanıt ver:
            {
              "filesToRead": ["path/to/file1.cs", "path/to/file2.ts"]
            }
            
            Kurallar:
            - Maksimum 10 dosya seçebilirsin
            - Sadece plan için en kritik dosyaları seç
            - Binary dosyalar (resim, font vb.) seçme
            - Eğer dosya okumana gerek yoksa boş liste döndür: { "filesToRead": [] }
            """;

            var discoveryRequest = new AiRequest
            {
                RequestId = Guid.NewGuid(),
                SystemPrompt = "Sen bir dosya analiz asistanısın. Sadece JSON yanıt ver.",
                UserPrompt = discoveryPrompt,
                MaxTokens = 1024,
                Temperature = 0.3,
                PreferredProvider = aiService.ProviderType
            };

            var response = await aiService.GenerateContentAsync(discoveryRequest, cancellationToken);

            if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
            {
                _logger.LogWarning("File discovery phase failed, skipping multi-turn");
                return null;
            }

            // Parse AI response for file list
            var content = response.Content.Trim();
            // JSON bloğunu temizle (```json ... ``` wrapper'ı varsa)
            if (content.StartsWith("```"))
            {
                var lines = content.Split('\n');
                content = string.Join("\n", lines.Skip(1).SkipLast(1));
            }

            var parsed = JsonSerializer.Deserialize<FileDiscoveryResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed?.FilesToRead != null && parsed.FilesToRead.Count > 0)
            {
                // Max 10 dosya sınırı
                return parsed.FilesToRead.Take(10).ToList();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "File discovery phase error, skipping multi-turn");
            return null;
        }
    }

    private record FileDiscoveryResponse(List<string>? FilesToRead);

    /// <summary>
    /// Publishes AI processing progress event for real-time updates via SignalR
    /// LogEntries ve RequestedFiles ile live log desteği sağlar
    /// </summary>
    private async Task PublishProgressAsync(
        GenerateAiPlanCommand request,
        string message,
        int progressPercentage,
        List<string>? logEntries = null,
        List<string>? requestedFiles = null)
    {
        try
        {
            var progressEvent = new AiProcessingProgress(
                RequestId: request.RequestId,
                ProjectId: request.ProjectId,
                UserId: request.UserId,
                Message: message,
                ProgressPercentage: progressPercentage,
                Timestamp: DateTime.UtcNow,
                LogEntries: logEntries?.ToList(), // Snapshot of current logs
                RequestedFiles: requestedFiles
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
