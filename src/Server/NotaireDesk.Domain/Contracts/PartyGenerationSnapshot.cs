using NotaireDesk.Domain.Parties;

namespace NotaireDesk.Domain.Contracts;

/// <summary>
/// Immutable-at-the-domain-layer copy of the party fields used in one generated
/// version of a contract. Identity documents can be added after encryption policy
/// and access controls have been designed.
/// </summary>
public sealed class PartyGenerationSnapshot
{
    private PartyGenerationSnapshot() { }

    internal PartyGenerationSnapshot(Guid sourcePartyId, ContractPartyRole role, PartyKind kind, string nameAr, string? nameFr)
    {
        SourcePartyId = sourcePartyId;
        Role = role;
        Kind = kind;
        NameAr = nameAr;
        NameFr = nameFr;
    }

    public Guid SourcePartyId { get; private set; }
    public ContractPartyRole Role { get; private set; }
    public PartyKind Kind { get; private set; }
    public string NameAr { get; private set; } = string.Empty;
    public string? NameFr { get; private set; }
}
