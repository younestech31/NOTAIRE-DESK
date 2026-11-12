namespace NotaireDesk.Domain.Parties;

/// <summary>
/// A reusable office party record. Contract output must use a generation-time
/// snapshot rather than reading this mutable master record at print time.
/// </summary>
public sealed class Party
{
    private Party() { }

    private Party(Guid id, PartyKind kind, string displayNameAr, string? displayNameFr, DateTimeOffset createdAt)
    {
        Id = id;
        Kind = kind;
        DisplayNameAr = displayNameAr;
        DisplayNameFr = displayNameFr;
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public PartyKind Kind { get; private set; }
    public string DisplayNameAr { get; private set; } = string.Empty;
    public string? DisplayNameFr { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Party Create(PartyKind kind, string displayNameAr, string? displayNameFr, DateTimeOffset createdAt)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayNameAr);
        return new Party(Guid.NewGuid(), kind, displayNameAr.Trim(), NormalizeOptional(displayNameFr), createdAt);
    }

    public void UpdateNames(string displayNameAr, string? displayNameFr, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayNameAr);
        var timestamp = updatedAt.ToUniversalTime();
        if (timestamp < CreatedAt)
            throw new ArgumentOutOfRangeException(nameof(updatedAt), "Update time cannot precede creation time.");

        DisplayNameAr = displayNameAr.Trim();
        DisplayNameFr = NormalizeOptional(displayNameFr);
        UpdatedAt = timestamp;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
