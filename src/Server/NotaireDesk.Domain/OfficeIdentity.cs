namespace NotaireDesk.Domain;

/// <summary>Office boundary identity; never accept tenant selection from an untrusted client.</summary>
public sealed record OfficeIdentity(Guid Id, string DisplayName)
{
    public static OfficeIdentity Create(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new OfficeIdentity(Guid.NewGuid(), displayName.Trim());
    }
}
