namespace NotaireDesk.Domain.Security;

/// <summary>A permission-bearing role within one office database.</summary>
public sealed class OfficeRole
{
    private readonly List<OfficeRolePermission> _permissions = [];

    private OfficeRole() { }

    private OfficeRole(
        Guid id,
        string code,
        string nameAr,
        string? nameFr,
        bool isSystemRole,
        DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        NameAr = nameAr;
        NameFr = nameFr;
        IsSystemRole = isSystemRole;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameFr { get; private set; }
    public bool IsSystemRole { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OfficeRolePermission> Permissions => _permissions.AsReadOnly();

    public static OfficeRole Create(
        string code,
        string nameAr,
        string? nameFr,
        bool isSystemRole,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        var normalizedCode = code.Trim().ToUpperInvariant();
        if (normalizedCode.Length is < 3 or > 50 || normalizedCode.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '_')))
            throw new ArgumentException("Role code must contain 3 to 50 ASCII letters, digits or underscores.", nameof(code));

        return new OfficeRole(
            Guid.NewGuid(), normalizedCode, nameAr.Trim(),
            string.IsNullOrWhiteSpace(nameFr) ? null : nameFr.Trim(),
            isSystemRole, createdAt);
    }

    public void AddPermission(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        if (!PermissionCodes.All.Contains(permission))
            throw new ArgumentException("Unknown permission code.", nameof(permission));
        if (_permissions.Any(x => x.Permission == permission))
            return;
        _permissions.Add(new OfficeRolePermission(Id, permission));
    }

    public void RemovePermission(string permission)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System roles are managed by the application.");
        var existing = _permissions.SingleOrDefault(x => x.Permission == permission);
        if (existing is not null)
            _permissions.Remove(existing);
    }
}
