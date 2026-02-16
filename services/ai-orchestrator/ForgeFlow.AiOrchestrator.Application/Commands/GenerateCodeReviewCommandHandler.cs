using ForgeFlow.AiOrchestrator.Domain.Abstractions;
using ForgeFlow.AiOrchestrator.Domain.Enums;
using ForgeFlow.AiOrchestrator.Domain.Models;
using ForgeFlow.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.AiOrchestrator.Application.Commands;

/// <summary>
/// AI Code Review handler.
/// PR diff'ini analiz eder ve yapılandırılmış review üretir.
/// GenerateAiPlanCommandHandler pattern'ını takip eder.
/// </summary>
public class GenerateCodeReviewCommandHandler : IRequestHandler<GenerateCodeReviewCommand, GenerateCodeReviewResult>
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<GenerateCodeReviewCommandHandler> _logger;

    public GenerateCodeReviewCommandHandler(
        IAiServiceFactory aiServiceFactory,
        IPublishEndpoint publishEndpoint,
        ILogger<GenerateCodeReviewCommandHandler> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<GenerateCodeReviewResult> Handle(GenerateCodeReviewCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Generating AI code review for Issue={IssueKey}, PR=#{PullNumber}, Project={ProjectId}",
            request.IssueKey, request.PullNumber, request.ProjectId);

        // 1. Build prompts
        var (systemPrompt, userPrompt) = BuildPrompts(request);

        // 2. Get AI service (default provider)
        AiProviderType? preferredProvider = null;
        if (!string.IsNullOrEmpty(request.PreferredProvider))
        {
            if (Enum.TryParse<AiProviderType>(request.PreferredProvider, ignoreCase: true, out var parsed))
                preferredProvider = parsed;
        }

        var aiService = _aiServiceFactory.GetService(preferredProvider);

        _logger.LogInformation("Code review using AI provider: {Provider}, Model: {Model}",
            aiService.ProviderType, aiService.ModelName);

        // 3. Generate review
        var aiRequest = new AiRequest
        {
            RequestId = request.RequestId,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            MaxTokens = 8192,
            Temperature = 0.3, // Düşük temperature → daha tutarlı review
            PreferredProvider = preferredProvider
        };

        var response = await aiService.GenerateContentAsync(aiRequest, cancellationToken);

        // 4. Return result
        if (response.IsSuccess)
        {
            _logger.LogInformation(
                "Code review generated successfully. Tokens: {Prompt}+{Completion}, Duration: {Duration}ms",
                response.PromptTokens, response.CompletionTokens, response.DurationMs);

            return GenerateCodeReviewResult.Success(response);
        }
        else
        {
            _logger.LogWarning(
                "Code review generation failed: {ErrorCode} - {ErrorMessage}",
                response.ErrorCode, response.ErrorMessage);

            return GenerateCodeReviewResult.Failure(
                response.ErrorMessage ?? "Unknown error",
                response.ErrorCode ?? "UNKNOWN",
                response.Provider);
        }
    }

    private static (string SystemPrompt, string UserPrompt) BuildPrompts(GenerateCodeReviewCommand request)
    {
        var systemPrompt = """
        You are a senior code reviewer working on a software development team.
        Your task is to analyze pull request diffs and provide structured code reviews.
        
        ## Review Principles:
        - Focus on code quality, security, performance, and maintainability
        - Be constructive and provide specific suggestions
        - Identify potential bugs and edge cases
        - Check for security vulnerabilities
        - Evaluate naming conventions and code readability
        - If an original implementation plan is provided, check compliance
        
        ## Output Format:
        Your response MUST be valid raw JSON only (no markdown code blocks).
        Use the exact structure specified in the user prompt.
        Escape all newlines within strings.
        """;

        // Plan uygunluk bölümü (plan varsa)
        var planSection = !string.IsNullOrEmpty(request.OriginalPlanJson)
            ? $"""
              
              ### ORIGINAL IMPLEMENTATION PLAN
              The following is the AI-generated implementation plan for this issue.
              Compare the PR changes against this plan and provide a compliance score.
              
              {request.OriginalPlanJson}
              """
            : """
              
              ### ORIGINAL PLAN
              No implementation plan available for this issue.
              Set planComplianceScore to null in your response.
              """;

        // JSON template'i ayrı string olarak tanımla (interpolation escape sorunlarını önlemek için)
        const string jsonTemplate = """
        {
          "summary": "4-5 sentence summary of the changes, their purpose, and overall quality assessment",
          "overallRating": "APPROVE | REQUEST_CHANGES | COMMENT",
          "codeQualityScore": 85,
          "planComplianceScore": 90,
          "findings": [
            {
              "severity": "error | warning | info | suggestion",
              "category": "bug | security | performance | style | maintainability",
              "file": "path/to/file.ts",
              "line": 23,
              "message": "Clear description of the issue",
              "suggestion": "How to fix or improve it"
            }
          ],
          "metrics": {
            "filesReviewed": 5,
            "totalAdditions": 120,
            "totalDeletions": 30,
            "criticalIssues": 0,
            "warnings": 2,
            "suggestions": 3
          }
        }
        """;

        var userPrompt = $"""
        Review the following Pull Request diff:

        ### PR INFORMATION
        - PR Title: {request.PrTitle ?? "N/A"}
        - PR Description: {request.PrDescription ?? "N/A"}
        - Issue: {request.IssueKey}
        - PR Number: #{request.PullNumber}

        ### DIFF CONTENT
        {TruncateDiff(request.DiffContent, 15000)}

        {planSection}

        ### EXPECTED JSON RESPONSE
        {jsonTemplate}
        """;

        return (systemPrompt, userPrompt);
    }

    /// <summary>
    /// Diff çok uzunsa truncate eder — token limiti aşmamak için
    /// </summary>
    private static string TruncateDiff(string diff, int maxChars)
    {
        if (string.IsNullOrEmpty(diff) || diff.Length <= maxChars)
            return diff;

        var truncated = diff[..maxChars];
        return truncated + "\n\n... [DIFF TRUNCATED — remaining content omitted for token efficiency]";
    }
}
