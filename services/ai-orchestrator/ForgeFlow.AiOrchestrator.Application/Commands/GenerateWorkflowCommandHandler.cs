using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Domain.Models;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ForgeFlow.AiOrchestrator.Application.Commands;

/// <summary>
/// GitHub Actions Workflow YAML üretim handler'ı.
/// Repo yapısını analiz ederek projeye uygun CI/CD pipeline YAML dosyası oluşturur.
/// Multi-Turn AI pattern kullanır: önce tree analizi, sonra istenen dosyaları okuyup YAML üretimi.
/// </summary>
public class GenerateWorkflowCommandHandler : IRequestHandler<GenerateWorkflowCommand, GenerateWorkflowResult>
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly IContextProvider _contextProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<GenerateWorkflowCommandHandler> _logger;

    public GenerateWorkflowCommandHandler(
        IAiServiceFactory aiServiceFactory,
        IContextProvider contextProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<GenerateWorkflowCommandHandler> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _contextProvider = contextProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<GenerateWorkflowResult> Handle(GenerateWorkflowCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating workflow for Project={ProjectId}, Provider={Provider}",
            request.ProjectId, request.PreferredProvider ?? "Default");

        var logEntries = new List<string>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Progress: Starting
            logEntries.Add("[START] GitHub Actions Workflow üretimi başlatılıyor...");
            await PublishProgressAsync(request, "Workflow üretimi başlatılıyor...", 5, logEntries);

            // 1. Parse provider
            AiProviderType? preferredProvider = null;
            if (!string.IsNullOrEmpty(request.PreferredProvider) &&
                Enum.TryParse<AiProviderType>(request.PreferredProvider, ignoreCase: true, out var parsed))
            {
                preferredProvider = parsed;
            }

            var aiService = _aiServiceFactory.GetService(preferredProvider);
            logEntries.Add($"[AI] Provider: {aiService.ProviderType}, Model: {aiService.ModelName}");

            // 2. Repo analiz bilgilerini logla
            logEntries.Add($"[REPO] Dosya ağacı: {request.TreePaths.Length} dosya");
            logEntries.Add($"[REPO] Kritik dosya: {request.CriticalFiles.Count} adet");
            logEntries.Add($"[REPO] Tech Stack: {string.Join(", ", request.DetectedTechStack)}");
            logEntries.Add($"[REPO] Mevcut workflow: {request.ExistingWorkflows.Length} adet");
            await PublishProgressAsync(request, "Repo analiz sonuçları hazır", 15, logEntries);

            // 3. Multi-Turn Phase 1: AI'a tree gönder, ek dosya isteyip istemediğini sor
            if (request.TreePaths.Length > 0)
            {
                logEntries.Add("[PHASE-1] AI'a dosya ağacı gönderiliyor...");
                await PublishProgressAsync(request, "AI dosya ağacını analiz ediyor...", 25, logEntries);

                var additionalFiles = await ExecuteDiscoveryPhase(aiService, request, cancellationToken);

                if (additionalFiles != null && additionalFiles.Count > 0)
                {
                    logEntries.Add($"[PHASE-1] AI {additionalFiles.Count} ek dosya istiyor: {string.Join(", ", additionalFiles.Take(5))}");
                    await PublishProgressAsync(request,
                        $"AI {additionalFiles.Count} dosya okumak istiyor...", 35,
                        logEntries, requestedFiles: additionalFiles);

                    // Phase 2: İstenen dosyaları çek
                    logEntries.Add("[PHASE-2] İstenen dosyalar GitHub'dan çekiliyor...");
                    await PublishProgressAsync(request, "İstenen dosyalar çekiliyor...", 40, logEntries);

                    var fetchedFiles = await _contextProvider.FetchRequestedFilesAsync(
                        request.ProjectId, additionalFiles.ToArray(), cancellationToken);

                    // Çekilen dosyaları CriticalFiles'a ekle
                    foreach (KeyValuePair<string, string> kvp in fetchedFiles)
                    {
                        request.CriticalFiles.TryAdd(kvp.Key, kvp.Value);
                    }

                    logEntries.Add($"[PHASE-2] ✅ {fetchedFiles.Count} dosya okundu");
                    await PublishProgressAsync(request, $"{fetchedFiles.Count} dosya okundu", 50, logEntries);
                }
            }

            // 4. Workflow YAML üretimi için prompt oluştur
            logEntries.Add("[GENERATE] Workflow prompt hazırlanıyor...");
            await PublishProgressAsync(request, "Workflow prompt hazırlanıyor...", 55, logEntries);

            var (systemPrompt, userPrompt) = BuildWorkflowPrompts(request);

            // 5. AI'a gönder
            logEntries.Add($"[GENERATE] AI modeline ({aiService.ModelName}) gönderiliyor...");
            await PublishProgressAsync(request, $"AI'a gönderiliyor ({aiService.ModelName})...", 65, logEntries);

            var aiRequest = new AiRequest
            {
                RequestId = request.RequestId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                MaxTokens = 4096,
                Temperature = 0.5,
                PreferredProvider = preferredProvider
            };

            var response = await aiService.GenerateContentAsync(aiRequest, cancellationToken);

            sw.Stop();
            logEntries.Add($"[GENERATE] AI yanıtı alındı ({response.DurationMs}ms)");
            await PublishProgressAsync(request, "AI yanıtı alındı, işleniyor...", 85, logEntries);

            if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
            {
                logEntries.Add($"[ERROR] ❌ Hata: {response.ErrorCode} - {response.ErrorMessage}");
                await PublishProgressAsync(request, $"Hata: {response.ErrorMessage}", 100, logEntries);

                return GenerateWorkflowResult.Failure(
                    response.ErrorMessage ?? "AI response was empty",
                    response.ErrorCode ?? "AI_ERROR");
            }

            // 6. YAML'ı parse et (AI response'tan ```yaml ... ``` bloğunu çıkar)
            var workflowYaml = ExtractYamlContent(response.Content);
            var workflowFileName = DetermineWorkflowFileName(request.DetectedTechStack);

            logEntries.Add($"[SUCCESS] ✅ Workflow YAML üretildi! Dosya: {workflowFileName}");
            logEntries.Add($"[SUCCESS] Tokens: {response.PromptTokens}+{response.CompletionTokens}, Süre: {sw.ElapsedMilliseconds}ms");
            await PublishProgressAsync(request, "Workflow başarıyla oluşturuldu!", 100, logEntries);

            return GenerateWorkflowResult.Success(
                workflowYaml,
                workflowFileName,
                aiService.ProviderType.ToString(),
                response.PromptTokens,
                response.CompletionTokens,
                sw.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Workflow generation failed for project {ProjectId}", request.ProjectId);

            logEntries.Add($"[ERROR] ❌ Beklenmeyen hata: {ex.Message}");
            await PublishProgressAsync(request, $"Hata: {ex.Message}", 100, logEntries);

            return GenerateWorkflowResult.Failure(ex.Message, "UNEXPECTED_ERROR");
        }
    }

    /// <summary>
    /// Phase 1: AI'a repo ağacını gösterip ek dosya isteğini sor
    /// </summary>
    private async Task<List<string>?> ExecuteDiscoveryPhase(
        IAiService aiService, GenerateWorkflowCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var treeSnippet = string.Join("\n", request.TreePaths.Take(300));
            var existingWorkflows = request.ExistingWorkflows.Length > 0
                ? string.Join(", ", request.ExistingWorkflows)
                : "Yok";

            var prompt = $$"""
            GitHub Actions CI/CD workflow YAML dosyası oluşturacaksın.
            Önce proje yapısını incele ve workflow oluşturmak için hangi dosyaları okuman gerektiğini belirle.
            
            ### Proje Tech Stack
            {{string.Join(", ", request.DetectedTechStack)}}
            
            ### Mevcut Workflow'lar
            {{existingWorkflows}}
            
            ### Dosya Ağacı (ilk 300 dosya)
            ```
            {{treeSnippet}}
            ```
            
            SADECE JSON formatında yanıt ver:
            {
              "filesToRead": ["path/to/file1", "path/to/file2"]
            }
            
            Kurallar:
            - Maksimum 10 dosya seç
            - package.json, Dockerfile, docker-compose.yml, *.csproj gibi build/config dosyalarını seç
            - Eğer zaten CriticalFiles'da varsa tekrar isteme
            - Eğer dosya okumana gerek yoksa boş liste döndür
            """;

            var aiReq = new AiRequest
            {
                RequestId = Guid.NewGuid(),
                SystemPrompt = "Sen bir CI/CD uzmanısın. Sadece JSON yanıt ver.",
                UserPrompt = prompt,
                MaxTokens = 1024,
                Temperature = 0.3,
                PreferredProvider = aiService.ProviderType
            };

            var resp = await aiService.GenerateContentAsync(aiReq, cancellationToken);
            if (!resp.IsSuccess || string.IsNullOrEmpty(resp.Content)) return null;

            var content = resp.Content.Trim();
            if (content.StartsWith("```"))
            {
                var lines = content.Split('\n');
                content = string.Join("\n", lines.Skip(1).SkipLast(1));
            }

            var parsed = JsonSerializer.Deserialize<FileDiscoveryResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed?.FilesToRead?.Take(10).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Workflow discovery phase failed, continuing without additional files");
            return null;
        }
    }

    private record FileDiscoveryResponse(List<string>? FilesToRead);

    /// <summary>
    /// Workflow YAML üretimi için system + user prompt'ları oluşturur
    /// </summary>
    private (string SystemPrompt, string UserPrompt) BuildWorkflowPrompts(GenerateWorkflowCommand request)
    {
        var systemPrompt = """
        Sen uzman bir DevOps mühendisisini ve GitHub Actions CI/CD pipeline tasarımcısısın.
        Verilen proje yapısı ve tech stack bilgilerine göre en uygun GitHub Actions workflow YAML dosyasını üret.
        
        Kurallar:
        1. Geçerli ve çalışır durumda YAML üret
        2. Projenin tech stack'ine uygun adımlar ekle (build, test, lint, deploy)
        3. Best practice'leri uygula (caching, matrix builds, environment secrets)
        4. PR ve push event'lerinde tetiklenmeli
        5. Güvenlik taramalarını dahil et (dependency check, code scanning)
        6. Yanıtını SADECE ```yaml ... ``` bloğu içinde ver
        7. Workflow dosya adını önermek için yorum satırı ekle
        """;

        var userPromptBuilder = new StringBuilder();
        userPromptBuilder.AppendLine("## Proje Bilgileri");
        userPromptBuilder.AppendLine();
        userPromptBuilder.AppendLine($"**Tech Stack:** {string.Join(", ", request.DetectedTechStack)}");
        userPromptBuilder.AppendLine($"**Toplam dosya sayısı:** {request.TreePaths.Length}");
        userPromptBuilder.AppendLine();

        // Mevcut workflow'lar
        if (request.ExistingWorkflows.Length > 0)
        {
            userPromptBuilder.AppendLine("## Mevcut Workflow Dosyaları");
            foreach (var wf in request.ExistingWorkflows)
            {
                userPromptBuilder.AppendLine($"- {wf}");
                if (request.CriticalFiles.TryGetValue(wf, out var wfContent))
                {
                    userPromptBuilder.AppendLine($"```yaml\n{wfContent}\n```");
                }
            }
            userPromptBuilder.AppendLine();
        }

        // Kritik dosya içerikleri
        userPromptBuilder.AppendLine("## Kritik Dosya İçerikleri");
        foreach (var (path, content) in request.CriticalFiles)
        {
            if (request.ExistingWorkflows.Contains(path)) continue; // Zaten yukarıda gösterildi
            
            var language = System.IO.Path.GetExtension(path).TrimStart('.') switch
            {
                "json" => "json",
                "yml" or "yaml" => "yaml",
                "csproj" or "cs" => "xml",
                "ts" or "tsx" => "typescript",
                "js" or "jsx" => "javascript",
                _ => ""
            };

            // Dosya içeriğini 500 satırla sınırla
            var truncatedContent = TruncateContent(content, 500);
            userPromptBuilder.AppendLine($"### {path}");
            userPromptBuilder.AppendLine($"```{language}\n{truncatedContent}\n```");
            userPromptBuilder.AppendLine();
        }

        // Dosya ağacı
        userPromptBuilder.AppendLine("## Dosya Ağacı (önemli dizinler)");
        userPromptBuilder.AppendLine("```");
        var importantPaths = request.TreePaths
            .Where(p => !p.Contains("node_modules/") && !p.Contains("bin/") && !p.Contains("obj/"))
            .Take(200);
        foreach (var path in importantPaths)
        {
            userPromptBuilder.AppendLine(path);
        }
        userPromptBuilder.AppendLine("```");

        userPromptBuilder.AppendLine();
        userPromptBuilder.AppendLine("Yukarıdaki bilgilere dayanarak bu proje için en uygun GitHub Actions CI/CD workflow YAML dosyasını oluştur.");

        return (systemPrompt, userPromptBuilder.ToString());
    }

    /// <summary>
    /// AI response'tan YAML içeriğini çıkarır
    /// </summary>
    private static string ExtractYamlContent(string aiResponse)
    {
        // ```yaml ... ``` bloğunu bul
        var startMarker = "```yaml";
        var endMarker = "```";

        var startIdx = aiResponse.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIdx >= 0)
        {
            startIdx += startMarker.Length;
            var endIdx = aiResponse.IndexOf(endMarker, startIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx > startIdx)
            {
                return aiResponse[startIdx..endIdx].Trim();
            }
        }

        // ```yml ... ``` de dene
        startMarker = "```yml";
        startIdx = aiResponse.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIdx >= 0)
        {
            startIdx += startMarker.Length;
            var endIdx = aiResponse.IndexOf(endMarker, startIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx > startIdx)
            {
                return aiResponse[startIdx..endIdx].Trim();
            }
        }

        // YAML bloğu bulunamazsa tüm response'u döndür
        return aiResponse.Trim();
    }

    /// <summary>
    /// Tech stack'e göre uygun workflow dosya adı belirler
    /// </summary>
    private static string DetermineWorkflowFileName(string[] techStack)
    {
        if (techStack.Any(t => t.Contains(".NET", StringComparison.OrdinalIgnoreCase)))
            return "ci-cd.yml";

        if (techStack.Any(t => t.Equals("React", StringComparison.OrdinalIgnoreCase) ||
                               t.Equals("Vue", StringComparison.OrdinalIgnoreCase) ||
                               t.Equals("Angular", StringComparison.OrdinalIgnoreCase)))
            return "frontend-ci.yml";

        if (techStack.Any(t => t.Equals("Docker", StringComparison.OrdinalIgnoreCase)))
            return "docker-build.yml";

        return "ci.yml";
    }

    /// <summary>
    /// Dosya içeriğini belirtilen satır sayısına kısaltır
    /// </summary>
    private static string TruncateContent(string content, int maxLines)
    {
        var lines = content.Split('\n');
        if (lines.Length <= maxLines) return content;

        var truncated = string.Join("\n", lines.Take(maxLines));
        return truncated + $"\n... ({lines.Length - maxLines} more lines truncated)";
    }

    /// <summary>
    /// SignalR üzerinden real-time progress event'i publish eder
    /// </summary>
    private async Task PublishProgressAsync(
        GenerateWorkflowCommand request,
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
                LogEntries: logEntries?.ToList(),
                RequestedFiles: requestedFiles
            );

            await _publishEndpoint.Publish(progressEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish workflow progress: {Message}", message);
        }
    }
}
