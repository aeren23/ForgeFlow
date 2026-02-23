using System.Text.Json;
using ForgeFlow.Artifact.Application.Abstractions;
using ForgeFlow.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ArtifactEntity = ForgeFlow.Artifact.Domain.Entities.Artifact;

namespace ForgeFlow.Artifact.Api.Consumers;

/// <summary>
/// CI/CD pipeline sonuçlarını versiyonlu artifact olarak saklar.
/// Sadece tamamlanmış (completed) pipeline sonuçlarını kaydeder.
/// CorrelationId: "run-{RunId}" ile idempotent kontrol sağlar.
/// Race condition'a dayanıklı: concurrent event'ler aynı artifact'ı
/// oluşturmaya çalışırsa, duplicate key hatası yakalanıp retry yapılır.
/// </summary>
public class CiCdStatusReceivedConsumer : IConsumer<CiCdStatusReceived>
{
    private readonly IArtifactRepository _repo;
    private readonly ILogger<CiCdStatusReceivedConsumer> _logger;

    public CiCdStatusReceivedConsumer(IArtifactRepository repo, ILogger<CiCdStatusReceivedConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CiCdStatusReceived> context)
    {
        var msg = context.Message;

        // Sadece tamamlanmış pipeline sonuçlarını artifact olarak sakla
        if (msg.Status != "completed")
        {
            _logger.LogDebug(
                "Skipping non-completed CI/CD status for artifact storage: Issue={IssueKey}, Status={Status}",
                msg.IssueKey, msg.Status);
            return;
        }

        var correlationId = $"run-{msg.RunId}";

        _logger.LogInformation(
            "Storing CI/CD result as artifact: Issue={IssueKey}, Workflow={Workflow}, Conclusion={Conclusion}",
            msg.IssueKey, msg.WorkflowName, msg.Conclusion);

        try
        {
            // Idempotency: Aynı RunId ile daha önce kaydedilmiş mi?
            if (await _repo.RevisionExistsByCorrelationIdAsync(correlationId, context.CancellationToken))
            {
                _logger.LogInformation("CI/CD artifact already exists for RunId={RunId}, skipping", msg.RunId);
                return;
            }

            // CI/CD sonuç içeriğini JSON olarak oluştur
            var content = JsonSerializer.Serialize(new
            {
                workflowName = msg.WorkflowName,
                conclusion = msg.Conclusion,
                commitSha = msg.CommitSha,
                branchName = msg.BranchName,
                htmlUrl = msg.HtmlUrl,
                runId = msg.RunId,
                repositoryId = msg.RepositoryId,
                timestamp = msg.Timestamp
            });

            var contentHash = ComputeHash(content);
            var metadata = JsonSerializer.Serialize(new { conclusion = msg.Conclusion });

            await StoreWithRetry(msg, correlationId, content, contentHash, metadata, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store CI/CD artifact for Issue={IssueKey}", msg.IssueKey);
        }
    }

    /// <summary>
    /// Race condition'a dayanıklı saklama. İki concurrent event aynı anda
    /// FindAsync → null alıp ikisi de INSERT denerse, ikincisi DbUpdateException
    /// alır. Bu durumda change tracker temizlenip mevcut artifact fetch edilir.
    /// </summary>
    private async Task StoreWithRetry(
        CiCdStatusReceived msg, string correlationId,
        string content, string contentHash, string metadata,
        CancellationToken ct)
    {
        var projectId = msg.RepositoryId.ToString();

        // İlk deneme: artifact'ı bul veya oluştur
        var cicdArtifact = await _repo.FindAsync(projectId, msg.IssueKey, "CI_CD_RESULT", ct);

        if (cicdArtifact == null)
        {
            cicdArtifact = new ArtifactEntity(
                projectId: projectId,
                issueId: msg.IssueKey,
                type: "CI_CD_RESULT"
            );
            _repo.Add(cicdArtifact);
        }

        var revision = cicdArtifact.AddRevision(
            contentJson: content,
            contentHash: contentHash,
            correlationId: correlationId,
            metadata: metadata
        );

        try
        {
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Stored CI/CD result artifact: Issue={IssueKey}, CorrelationId={CorrelationId}, Revision={RevisionNo}",
                msg.IssueKey, correlationId, revision.RevisionNo);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true
                                         || ex.InnerException?.Message.Contains("IX_Artifacts_ProjectId_IssueId_Type") == true)
        {
            _logger.LogWarning(
                "Concurrent artifact creation detected for Issue={IssueKey}, retrying with existing artifact",
                msg.IssueKey);

            // Change tracker'daki başarısız entity'leri temizle
            _repo.DetachAll();

            // Artık artifact kesinlikle var — tekrar çek
            cicdArtifact = await _repo.FindAsync(projectId, msg.IssueKey, "CI_CD_RESULT", ct);

            if (cicdArtifact == null)
            {
                _logger.LogError("Artifact still not found after duplicate key error for Issue={IssueKey}", msg.IssueKey);
                return;
            }

            // Revision ekle ve kaydet
            var retryRevision = cicdArtifact.AddRevision(
                contentJson: content,
                contentHash: contentHash,
                correlationId: correlationId,
                metadata: metadata
            );

            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Stored CI/CD result artifact (retry): Issue={IssueKey}, CorrelationId={CorrelationId}, Revision={RevisionNo}",
                msg.IssueKey, correlationId, retryRevision.RevisionNo);
        }
    }

    private static string ComputeHash(string content)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
