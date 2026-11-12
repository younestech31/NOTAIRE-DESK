namespace NotaireDesk.Domain.Security;

/// <summary>
/// Local office identity. Passwords are always stored as a password-hasher output;
/// this aggregate never accepts or stores plaintext credentials.
/// </summary>
public sealed class OfficeUser
{
    private readonly List<OfficeUserRole> _roles = [];

    private OfficeUser() { }

    private OfficeUser(
        Guid id,
        string username,
        string normalizedUsername,
        string displayNameAr,
        string? displayNameFr,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        Id = id;
        Username = username;
        NormalizedUsername = normalizedUsername;
        DisplayNameAr = displayNameAr;
        DisplayNameFr = displayNameFr;
        PasswordHash = passwordHash;
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string DisplayNameAr { get; private set; } = string.Empty;
    public string? DisplayNameFr { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockoutEndUtc { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<OfficeUserRole> Roles => _roles.AsReadOnly();

    public bool IsLockedOut(DateTimeOffset now) =>
        LockoutEndUtc.HasValue && LockoutEndUtc.Value > now.ToUniversalTime();

    public static OfficeUser Create(
        string username,
        string displayNameAr,
        string? displayNameFr,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayNameAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        var trimmed = username.Trim();
        if (trimmed.Length is < 3 or > 80)
            throw new ArgumentOutOfRangeException(nameof(username), "Username must contain 3 to 80 characters.");
        if (trimmed.Any(char.IsWhiteSpace))
            throw new ArgumentException("Username cannot contain whitespace.", nameof(username));
        if (passwordHash.Length > 1000)
            throw new ArgumentOutOfRangeException(nameof(passwordHash));

        return new OfficeUser(
            Guid.NewGuid(),
            trimmed,
            trimmed.ToUpperInvariant(),
            displayNameAr.Trim(),
            NormalizeOptional(displayNameFr),
            passwordHash,
            createdAt);
    }

    public void ReplacePasswordHash(string passwordHash, DateTimeOffset changedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        if (passwordHash.Length > 1000)
            throw new ArgumentOutOfRangeException(nameof(passwordHash));
        PasswordHash = passwordHash;
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        Touch(changedAt);
    }

    public void UpdateDisplayNames(string displayNameAr, string? displayNameFr, DateTimeOffset changedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayNameAr);
        DisplayNameAr = displayNameAr.Trim();
        DisplayNameFr = NormalizeOptional(displayNameFr);
        Touch(changedAt);
    }

    public bool RecordFailedLogin(DateTimeOffset now, int maximumAttempts, TimeSpan lockoutDuration)
    {
        if (maximumAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        if (lockoutDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));

        var timestamp = now.ToUniversalTime();
        if (!IsActive || IsLockedOut(timestamp))
            return IsLockedOut(timestamp);

        FailedLoginAttempts = checked(FailedLoginAttempts + 1);
        if (FailedLoginAttempts >= maximumAttempts)
        {
            LockoutEndUtc = timestamp.Add(lockoutDuration);
            FailedLoginAttempts = 0;
            Touch(timestamp);
            return true;
        }

        Touch(timestamp);
        return false;
    }

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        Touch(now);
    }

    public void Disable(DateTimeOffset changedAt)
    {
        IsActive = false;
        Touch(changedAt);
    }

    public void Enable(DateTimeOffset changedAt)
    {
        IsActive = true;
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        Touch(changedAt);
    }

    public void ReplaceRoles(IEnumerable<Guid> roleIds)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        var requestedRoleIds = roleIds.ToArray();
        var uniqueRoleIds = requestedRoleIds.Distinct().ToArray();
        if (uniqueRoleIds.Any(x => x == Guid.Empty) || uniqueRoleIds.Length != requestedRoleIds.Length)
            throw new ArgumentException("Role ids must be unique and non-empty.", nameof(roleIds));

        _roles.Clear();
        foreach (var roleId in uniqueRoleIds)
            _roles.Add(new OfficeUserRole(Id, roleId));
    }

    private void Touch(DateTimeOffset changedAt)
    {
        var timestamp = changedAt.ToUniversalTime();
        if (timestamp < CreatedAt)
            throw new ArgumentOutOfRangeException(nameof(changedAt), "Change time cannot precede creation time.");
        UpdatedAt = timestamp;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
