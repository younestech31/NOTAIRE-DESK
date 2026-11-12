using System.Security.Claims;
using NotaireDesk.Domain.Security;
using NotaireDesk.Infrastructure.Persistence;

namespace NotaireDesk.Api.Security;

public sealed class SecurityAuditWriter(OfficeDbContext db, IHttpContextAccessor accessor)
{
    public void Add(
        string action,
        string targetType,
        string? targetId,
        AuditOutcome outcome,
        string? safeMetadata = null,
        Guid? actorUserId = null,
        string? actorUsername = null)
    {
        var http = accessor.HttpContext;
        actorUserId ??= Guid.TryParse(http?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        actorUsername ??= http?.User.Identity?.Name;

        db.AuditEvents.Add(AuditEvent.Create(
            actorUserId,
            actorUsername,
            action,
            targetType,
            targetId,
            outcome,
            DateTimeOffset.UtcNow,
            http?.Connection.RemoteIpAddress?.ToString(),
            http?.TraceIdentifier,
            safeMetadata));
    }
}
