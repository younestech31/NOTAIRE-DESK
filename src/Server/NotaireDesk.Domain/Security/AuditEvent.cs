namespace NotaireDesk.Domain.Security;

public enum AuditOutcome
{
    Succeeded = 1,
    Failed = 2,
    Denied = 3
}

/// <summary>
/// Append-only security and business audit event. Do not put passwords, tokens,
/// national IDs, contract text or request bodies in this record.
/// </summary>
public sealed class AuditEvent
{
    private AuditEvent() { }

    private AuditEvent(
        Guid id,
        Guid? actorUserId,
        string? actorUsername,
        string action,
        string targetType,
        string? targetId,
        AuditOutcome outcome,
        DateTimeOffset occurredAt,
        string? clientAddress,
        string? correlationId,
        string? safeMetadata)
    {
        Id = id;
        ActorUserId = actorUserId;
        ActorUsername = actorUsername;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        Outcome = outcome;
        OccurredAt = occurredAt.ToUniversalTime();
        ClientAddress = clientAddress;
        CorrelationId = correlationId;
        SafeMetadata = safeMetadata;
    }

    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorUsername { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty;
    public string? TargetId { get; private set; }
    public AuditOutcome Outcome { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? ClientAddress { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? SafeMetadata { get; private set; }

    public static AuditEvent Create(
        Guid? actorUserId,
        string? actorUsername,
        string action,
        string targetType,
        string? targetId,
        AuditOutcome outcome,
        DateTimeOffset occurredAt,
        string? clientAddress = null,
        string? correlationId = null,
        string? safeMetadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetType);
        if (action.Length > 100)
            throw new ArgumentOutOfRangeException(nameof(action));
        if (targetType.Length > 80)
            throw new ArgumentOutOfRangeException(nameof(targetType));
        if (actorUsername?.Length > 80 || targetId?.Length > 120 ||
            clientAddress?.Length > 64 || correlationId?.Length > 100 ||
            safeMetadata?.Length > 2000)
            throw new ArgumentOutOfRangeException(nameof(safeMetadata), "Audit field exceeds its maximum length.");
        if (!Enum.IsDefined(outcome))
            throw new ArgumentOutOfRangeException(nameof(outcome));

        return new AuditEvent(
            Guid.NewGuid(), actorUserId,
            string.IsNullOrWhiteSpace(actorUsername) ? null : actorUsername.Trim(),
            action.Trim(), targetType.Trim(),
            string.IsNullOrWhiteSpace(targetId) ? null : targetId.Trim(),
            outcome, occurredAt, clientAddress, correlationId, safeMetadata);
    }
}
