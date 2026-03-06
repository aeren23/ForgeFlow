using ForgeFlow.AiOrchestrator.Application.Commands;
using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace ForgeFlow.AiOrchestrator.Worker.Consumers;

/// <summary>
/// WorkflowGenerationRequested event'ini tüketir.
/// GitHub Service'den repo analizi yapar, ardından GenerateWorkflowCommand'ı çalıştırır.
/// Sonucu WorkflowGenerationCompleted/Failed olarak publish eder.
/// </summary>
public class WorkflowGenerationRequestedConsumer : IConsumer<EventEnvelope<WorkflowGenerationRequested>>
{
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorkflowGenerationRequestedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public WorkflowGenerationRequestedConsumer(
        IMediator mediator,
        IPublishEndpoint publishEndpoint,
        HttpClient httpClient,
        ILogger<WorkflowGenerationRequestedConsumer> logger,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _publishEndpoint = publishEndpoint;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<EventEnvelope<WorkflowGenerationRequested>> context)
    {
        var envelope = context.Message;
        var msg = envelope.Data;

        _logger.LogInformation(
            "Received WorkflowGenerationRequested | Project={ProjectId} User={UserId} Provider={Provider}",
            msg.ProjectId, msg.RequestedByUserId, msg.PreferredProvider ?? "Default");

        try
        {
            // 1. GitHub Service'den repo analizi çek
            _logger.LogInformation("Fetching repo analysis for project {ProjectId}", msg.ProjectId);

            var githubServiceUrl = _configuration.GetValue<string>("Services:GitHubApiUrl")
                                   ?? "http://github:8080";
            var analyzeUrl = $"{githubServiceUrl}/api/repositories/{msg.ProjectId}/analyze";

            var response = await _httpClient.GetAsync(analyzeUrl, context.CancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Repo analysis failed: {StatusCode}", response.StatusCode);

                await _publishEndpoint.Publish(new EventEnvelope<WorkflowGenerationFailed>(
                    EventId: Guid.NewGuid(),
                    OccurredAtUtc: DateTime.UtcNow,
                    CorrelationId: envelope.CorrelationId,
                    UserId: msg.RequestedByUserId,
                    CausationId: envelope.EventId.ToString(),
                    Data: new WorkflowGenerationFailed(
                        msg.ProjectId,
                        "REPO_ANALYSIS_FAILED",
                        $"GitHub repository analysis failed with status {response.StatusCode}"
                    )
                ), context.CancellationToken);
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var analysis = await response.Content.ReadFromJsonAsync<RepoAnalysisResponse>(options, context.CancellationToken);

            if (analysis == null)
            {
                throw new InvalidOperationException("Repo analysis returned null");
            }

            _logger.LogInformation(
                "Repo analysis complete: {TreeCount} files, {CriticalCount} critical, TechStack=[{Tech}]",
                analysis.TreePaths?.Length ?? 0,
                analysis.CriticalFiles?.Count ?? 0,
                string.Join(", ", analysis.DetectedTechStack ?? []));

            // 2. GenerateWorkflowCommand oluştur
            var command = new GenerateWorkflowCommand
            {
                RequestId = envelope.EventId,
                ProjectId = Guid.TryParse(msg.ProjectId, out var pid) ? pid : Guid.Empty,
                UserId = msg.RequestedByUserId,
                PreferredProvider = msg.PreferredProvider,
                TreePaths = analysis.TreePaths ?? [],
                CriticalFiles = analysis.CriticalFiles ?? new Dictionary<string, string>(),
                DetectedTechStack = analysis.DetectedTechStack ?? [],
                ExistingWorkflows = analysis.ExistingWorkflows ?? []
            };

            // 3. Handler'ı çalıştır
            var result = await _mediator.Send(command, context.CancellationToken);

            // 4. Sonucu publish et
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Workflow generated successfully: {FileName} ({Duration}ms)",
                    result.WorkflowFileName, result.DurationMs);

                await _publishEndpoint.Publish(new EventEnvelope<WorkflowGenerationCompleted>(
                    EventId: Guid.NewGuid(),
                    OccurredAtUtc: DateTime.UtcNow,
                    CorrelationId: envelope.CorrelationId,
                    UserId: msg.RequestedByUserId,
                    CausationId: envelope.EventId.ToString(),
                    Data: new WorkflowGenerationCompleted(
                        msg.ProjectId,
                        result.WorkflowYaml!,
                        result.WorkflowFileName!,
                        result.UsedProvider!,
                        result.PromptTokens,
                        result.CompletionTokens,
                        result.DurationMs
                    )
                ), context.CancellationToken);
            }
            else
            {
                _logger.LogWarning("Workflow generation failed: {Error}", result.ErrorMessage);

                await _publishEndpoint.Publish(new EventEnvelope<WorkflowGenerationFailed>(
                    EventId: Guid.NewGuid(),
                    OccurredAtUtc: DateTime.UtcNow,
                    CorrelationId: envelope.CorrelationId,
                    UserId: msg.RequestedByUserId,
                    CausationId: envelope.EventId.ToString(),
                    Data: new WorkflowGenerationFailed(
                        msg.ProjectId,
                        result.ErrorCode ?? "GENERATION_FAILED",
                        result.ErrorMessage ?? "Unknown error"
                    )
                ), context.CancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in WorkflowGenerationRequestedConsumer");

            await _publishEndpoint.Publish(new EventEnvelope<WorkflowGenerationFailed>(
                EventId: Guid.NewGuid(),
                OccurredAtUtc: DateTime.UtcNow,
                CorrelationId: envelope.CorrelationId,
                UserId: msg.RequestedByUserId,
                CausationId: envelope.EventId.ToString(),
                Data: new WorkflowGenerationFailed(
                    msg.ProjectId,
                    "UNEXPECTED_ERROR",
                    ex.Message
                )
            ), context.CancellationToken);
        }
    }
}

/// <summary>
/// GitHub Service /api/repositories/{id}/analyze response model
/// </summary>
internal class RepoAnalysisResponse
{
    public string[]? TreePaths { get; set; }
    public Dictionary<string, string>? CriticalFiles { get; set; }
    public string[]? DetectedTechStack { get; set; }
    public string[]? ExistingWorkflows { get; set; }
}
