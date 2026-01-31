using ForgeFlow.Work.Application.Abstractions;
using ForgeFlow.Work.Application.Services;
using ForgeFlow.Work.Domain.Entities;
using ForgeFlow.Work.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ForgeFlow.Work.Application.Issues.Commands;

/// <summary>
/// Issue oluşturma handler
/// </summary>
public class CreateIssueHandler : IRequestHandler<CreateIssueCommand, CreateIssueResult>
{
    private readonly IWorkDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IProjectPermissionService _permissionService;

    public CreateIssueHandler(IWorkDbContext context, ICurrentUserService currentUser, IProjectPermissionService permissionService)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<CreateIssueResult> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        // Proje'yi bul ve issue numarasını artır
        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Key == request.ProjectKey.ToUpperInvariant(), cancellationToken)
            ?? throw new InvalidOperationException($"Project '{request.ProjectKey}' not found");



        // Yetki Kontrolü
        var currentUserId = _currentUser.UserId;
        var member = project.Members.FirstOrDefault(m => m.UserId == currentUserId);

        // Kullanıcı projede değilse ve Admin değilse hata ver (Viewer bile olsa üye olması lazım)
        // matrix: "Viewer: Read-only access". Ama projeye eklenmiş olması lazım.
        if (member == null && !_currentUser.IsInRole("Admin"))
        {
            // Projede olmayan biri Viewer bile olamaz (Public proje desteği yoksa)
            throw new UnauthorizedAccessException("You are not a member of this project.");
        }

        var role = member?.Role ?? ProjectRole.Viewer;
        // Admin ise her zaman izin ver (veya role override et) -> ProjectPermissionService içinde "Admin" rolü check ediliyor ama 
        // buradaki "role" ProjectRole enum'ı. "System Admin" ile "Project Admin" farklı.
        // System Admin her şeyi yapabilmeli mi? Evet.
        if (_currentUser.IsInRole("Admin"))
        {
            role = ProjectRole.Owner; // System Admin'e Owner muamelesi yap
        }

        // Permission Service kontrolü
        if (!_permissionService.CanCreateIssue(role))
        {
            throw new UnauthorizedAccessException($"Role '{role}' is not allowed to create issues.");
        }

        // Parent issue kontrolü
        Guid? parentIssueId = null;
        if (!string.IsNullOrEmpty(request.ParentIssueKey))
        {
            var parentIssue = await _context.Issues
                .FirstOrDefaultAsync(i => i.Key == request.ParentIssueKey.ToUpperInvariant(), cancellationToken)
                ?? throw new InvalidOperationException($"Parent issue '{request.ParentIssueKey}' not found");
            parentIssueId = parentIssue.Id;
        }

        // Issue key oluştur (Robust Implementation)
        // Desync durumunda (NextIssueNumber geride kalırsa) duplicate key hatası almamak için loop
        string issueKey;
        do
        {
            var issueNumber = project.NextIssueNumber;
            project.NextIssueNumber++; // Her denemede artır
            issueKey = $"{project.Key}-{issueNumber}";

            // Key kullanımda mı kontrol et
            // Not: Bu işlem her create'de ekstra bir query demek ama güvenli.
            // Eğer performans sorunu olursa unique constraint exception yakalanıp retry edilebilir.
            // Şimdilik temiz veri için bu daha iyi.
        }
        while (await _context.Issues.AnyAsync(i => i.Key == issueKey, cancellationToken));

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            Key = issueKey,
            Title = request.Title,
            Description = request.Description,
            Status = IssueStatus.Open,
            Priority = request.Priority,
            Type = request.Type,
            ProjectId = project.Id,
            ParentIssueId = parentIssueId,
            ReporterId = request.ReporterId,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate,
            EstimatedHours = request.EstimatedHours,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _context.Issues.Add(issue);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateIssueResult(issue.Id, issue.Key, issue.Title, issue.Status);
    }
}
