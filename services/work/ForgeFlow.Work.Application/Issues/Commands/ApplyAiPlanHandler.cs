using System.Text.Json;
using System.Text.Json.Serialization;
using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Domain.Entities;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Work.Application.Issues.Commands;

public class ApplyAiPlanHandler : IRequestHandler<ApplyAiPlanCommand, bool>
{
    private readonly IWorkDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ApplyAiPlanHandler> _logger;

    public ApplyAiPlanHandler(IWorkDbContext dbContext, IPublishEndpoint publishEndpoint, ILogger<ApplyAiPlanHandler> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<bool> Handle(ApplyAiPlanCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PlanJson))
        {
            _logger.LogWarning("Generated content is empty for Epic {EpicKey}", request.ParentIssueKey);
            return false;
        }

        try
        {
            var jsonContent = request.PlanJson.Trim();
            if (jsonContent.StartsWith("```json"))
            {
                jsonContent = jsonContent.Substring(7);
            }
            if (jsonContent.StartsWith("```"))
            {
                jsonContent = jsonContent.Substring(3);
            }
            if (jsonContent.EndsWith("```"))
            {
                jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
            }
            jsonContent = jsonContent.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var plan = JsonSerializer.Deserialize<AiPlanDto>(jsonContent, options);

            if (plan?.ImplementationPlan?.ListOfChanges == null)
            {
                _logger.LogWarning("Invalid plan format for Epic {EpicKey}", request.ParentIssueKey);
                return false;
            }

            // Epic'i bul
            var epic = await _dbContext.Issues
                .FirstOrDefaultAsync(i => i.Key == request.ParentIssueKey, cancellationToken);

            if (epic == null)
            {
                _logger.LogError("Parent Epic {EpicKey} not found!", request.ParentIssueKey);
                return false;
            }

            var project = await _dbContext.Projects
                .FirstOrDefaultAsync(p => p.Id == epic.ProjectId, cancellationToken);

            if (project == null) return false;

            // Her bir değişiklik maddesi için ayrı bir Story/Task oluştur
            foreach (var change in plan.ImplementationPlan.ListOfChanges)
            {
                project.NextIssueNumber++;
                var issueNumber = project.NextIssueNumber;
                var issueKey = $"{project.Key}-{issueNumber}";

                var newIssue = new Issue
                {
                    Key = issueKey,
                    // Title için 100 karakter limiti kontrolü
                    Title = change.Title.Length > 100 ? change.Title.Substring(0, 97) + "..." : change.Title,
                    Description = change.Description,
                    Type = IssueType.Story,
                    Status = IssueStatus.Open,
                    Priority = IssuePriority.Medium,
                    ProjectId = project.Id,
                    ParentIssueId = epic.Id,
                    ReporterId = request.UserId,
                    AssigneeId = request.UserId, // Tetikleyen kişiye ata
                    CreatedAtUtc = DateTime.UtcNow
                };

                _dbContext.Issues.Add(newIssue);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var createdCount = plan.ImplementationPlan.ListOfChanges.Count;
            _logger.LogInformation("{Count} stories created for Epic {EpicKey}", createdCount, request.ParentIssueKey);

            // Publish UserNotification for real-time feedback
            try
            {
                await _publishEndpoint.Publish(new UserNotification(
                    UserId: request.UserId,
                    Type: "ai_plan_complete",
                    Title: "AI Plan Uygulandı! 🎉",
                    Message: $"{createdCount} issue oluşturuldu ({request.ParentIssueKey})",
                    Data: new
                    {
                        ProjectId = request.ProjectId,
                        ParentIssueKey = request.ParentIssueKey,
                        CreatedCount = createdCount
                    }
                ), cancellationToken);
            }
            catch
            {
                // Don't fail if notification fails
            }

            // Publish Final Progress Log (100%)
            try
            {
                await _publishEndpoint.Publish(new AiProcessingProgress(
                    RequestId: request.RequestId,
                    ProjectId: Guid.TryParse(request.ProjectId, out var pid) ? pid : Guid.Empty,
                    UserId: request.UserId,
                    Message: $"AI plan başarıyla uygulandı! {createdCount} issue oluşturuldu.",
                    ProgressPercentage: 100,
                    Timestamp: DateTime.UtcNow
                ), cancellationToken);
            }
            catch
            {
                // Ignore log errors
            }

            return true;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON Parsing Failed for Epic {EpicKey}. Content Preview: {Content}",
                request.ParentIssueKey,
                request.PlanJson.Length > 500 ? request.PlanJson.Substring(0, 500) + "..." : request.PlanJson);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply AI Plan for Epic {EpicKey}", request.ParentIssueKey);
            return false;
        }
    }

    // JSON DTO Classes (Internal to Handler or Shared)
    private class AiPlanDto
    {
        [JsonPropertyName("summary")]
        public required string Summary { get; set; }

        [JsonPropertyName("implementation_plan")]
        public required ImplementationPlanDto ImplementationPlan { get; set; }
    }

    private class ImplementationPlanDto
    {
        [JsonPropertyName("summary")]
        public required string Summary { get; set; }

        [JsonPropertyName("list_of_changes")]
        public required List<StoryTaskDto> ListOfChanges { get; set; }
    }

    private class StoryTaskDto
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("description")]
        public required string Description { get; set; }
    }
}
