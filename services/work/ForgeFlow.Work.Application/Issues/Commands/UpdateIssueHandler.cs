using ForgeFlow.Contracts.Events;
using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Services;
using ForgeFlow.Work.Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue güncelleme handler
/// </summary>
public class UpdateIssueHandler : IRequestHandler<UpdateIssueCommand, UpdateIssueResult>
{
    private readonly IWorkDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectPermissionService _permissionService;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateIssueHandler(IWorkDbContext context, ICurrentUserService currentUser, IProjectPermissionService permissionService, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<UpdateIssueResult> Handle(UpdateIssueCommand request, CancellationToken cancellationToken)
    {
        // Yetki Hizmetini Kullan
        var issue = await _context.Issues
            .Include(i => i.Project)
            .ThenInclude(p => p.Members)
            .FirstOrDefaultAsync(i => i.Key == request.Key.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Issue '{request.Key}' not found");

        var currentUserId = _currentUser.UserId;
        var member = issue.Project.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // System Admin override
        var isSystemAdmin = _currentUser.IsInRole("Admin");
        var role = isSystemAdmin ? ProjectRole.Owner : (member?.Role ?? ProjectRole.Viewer);

        // 1. Assignee Değişiyor mu?
        if (issue.AssigneeId != request.AssigneeId)
        {
            // Atama yetkisi kontrolü
            // Target User ID (Yeni atanan kişi null ise, unassign demektir - buna kimin yetkisi var? Assign yetkisi olanın.)
            // Matrix: Member can assign ONLY to themselves.
            var targetUserId = request.AssigneeId;

            if (!_permissionService.CanAssignUser(role, targetUserId!, currentUserId))
            {
                throw new UnauthorizedAccessException($"Role '{role}' is not allowed to assign this issue to '{targetUserId}'.");
            }
        }

        // 2. Diğer alanlar değişiyor mu? (Edit)
        // Eğer sadece Assignee değişiyorsa UpdateIssueHandler kullanılıyor mu? Evet, UI hepsi tek formda olabilir.
        // Ama genelde Edit yetkisi de istenir.
        if (!_permissionService.CanEditIssue(role, issue.ReporterId, currentUserId))
        {
            throw new UnauthorizedAccessException($"Role '{role}' is not allowed to edit this issue details.");
        }

        var oldAssigneeId = issue.AssigneeId;

        issue.Title = request.Title;
        issue.Description = request.Description;
        issue.Type = request.Type;
        issue.Priority = request.Priority;
        issue.AssigneeId = request.AssigneeId;
        issue.DueDate = request.DueDate;
        issue.EstimatedHours = request.EstimatedHours;
        issue.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Eğer yeni bir atama yapıldıysa (Unassign durumu hariç veya ona da bakılabilir ama branch create için unassign önemiz)
        // Ve Assignee değiştiyse
        if (issue.AssigneeId != null && issue.AssigneeId != oldAssigneeId)
        {
            await _publishEndpoint.Publish(new IssueAssigned(
                issue.Key,
                issue.Title,
                issue.ProjectId,
                oldAssigneeId,
                issue.AssigneeId,
                issue.Project.RepositoryUrl ?? "",
                issue.Project.DefaultBranch,
                DateTime.UtcNow
            ), cancellationToken);
        }

        return new UpdateIssueResult(issue.Id, issue.Key, issue.Title);
    }
}
