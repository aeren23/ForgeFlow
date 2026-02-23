using MediatR;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// CI/CD pipeline durumunu issue'ya kaydeden komut.
/// CiCdStatusReceivedConsumer tarafından dispatch edilir.
/// </summary>
public record UpdateCiCdStatusCommand(
    string IssueKey,
    string WorkflowName,
    string Status,        // "queued", "in_progress", "success", "failure", "cancelled"
    string? HtmlUrl,
    string CommitSha,
    long RunId
) : IRequest<UpdateCiCdStatusResult>;

public record UpdateCiCdStatusResult(
    string IssueKey,
    Guid ProjectId,
    string Status
);
