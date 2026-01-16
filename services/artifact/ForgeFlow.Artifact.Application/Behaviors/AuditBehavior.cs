using ForgeFlow.Artifact.Application.Abstractions;
using ForgeFlow.Artifact.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ForgeFlow.Artifact.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior - IAuditLoggable implement eden tüm Command'ları yakalar ve audit log oluşturur.
/// Bu sayede iş mantığı ile denetim mantığı birbirine karışmaz (Separation of Concerns).
/// </summary>
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditLogRepository _auditRepository;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(IAuditLogRepository auditRepository, ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 1. İşlemi gerçekleştir (Command Handler'ı çalıştır)
        var response = await next();

        // 2. Sadece IAuditLoggable implement eden Command'ları logla
        if (request is IAuditLoggable loggable)
        {
            try
            {
                var auditEntry = new AuditLog(
                    entityName: loggable.EntityName,
                    entityId: loggable.EntityId,
                    action: loggable.Action,
                    userId: loggable.UserId,
                    correlationId: loggable.CorrelationId
                );

                await _auditRepository.AddAsync(auditEntry, cancellationToken);
                await _auditRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Audit log saved | Entity={EntityName} Action={Action} UserId={UserId} CorrelationId={CorrelationId}",
                    loggable.EntityName, loggable.Action, loggable.UserId, loggable.CorrelationId);
            }
            catch (Exception ex)
            {
                // Audit log hatası ana işlemi engellemez, sadece uyarı verilir
                _logger.LogWarning(ex,
                    "Failed to save audit log for {EntityName} by {UserId}",
                    loggable.EntityName, loggable.UserId);
            }
        }

        return response;
    }
}
