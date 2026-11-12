namespace NotaireDesk.Domain.Security;

/// <summary>
/// Persisted refresh-token session. Only a SHA-256 hash of the opaque refresh token
/// is retained; plaintext tokens are returned once to the authenticated client.
/// </summary>
public sealed class UserSession
{
    private UserSession() { }

    private UserSession(
        Guid id,
        Guid userId,
        string refreshTokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? deviceLabel)
    {
        Id = id;
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        CreatedAt = createdAt.ToUniversalTime();
        ExpiresAt = expiresAt.ToUniversalTime();
        DeviceLabel = string.IsNullOrWhiteSpace(deviceLabel) ? null : deviceLabel.Trim();
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string RefreshTokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }
    public string? DeviceLabel { get; private set; }
    public int Version { get; private set; }

    public bool IsActive(DateTimeOffset now) =>
        !RevokedAt.HasValue && ExpiresAt > now.ToUniversalTime();

    public static UserSession Create(
        Guid userId,
        string refreshTokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? deviceLabel)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenHash);
        if (refreshTokenHash.Length != 64 || refreshTokenHash.Any(c => !Uri.IsHexDigit(c)))
            throw new ArgumentException("Refresh token hash must be a 64-character SHA-256 hex digest.", nameof(refreshTokenHash));
        if (expiresAt <= createdAt)
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Session expiry must follow creation time.");

        return new UserSession(Guid.NewGuid(), userId, refreshTokenHash.ToLowerInvariant(), createdAt, expiresAt, deviceLabel);
    }

    public void Revoke(DateTimeOffset revokedAt, Guid? replacedBySessionId = null)
    {
        if (RevokedAt.HasValue)
            throw new InvalidOperationException("Session has already been revoked.");
        var timestamp = revokedAt.ToUniversalTime();
        if (timestamp < CreatedAt)
            throw new ArgumentOutOfRangeException(nameof(revokedAt));
        if (replacedBySessionId == Id)
            throw new ArgumentException("A session cannot replace itself.", nameof(replacedBySessionId));
        RevokedAt = timestamp;
        ReplacedBySessionId = replacedBySessionId;
        Version = checked(Version + 1);
    }
}
