namespace NotaireDesk.Domain.Contracts;

/// <summary>Links a party master record to a contract in a specific legal role.</summary>
public sealed class ContractPartyReference
{
    private ContractPartyReference() { }

    internal ContractPartyReference(Guid partyId, ContractPartyRole role)
    {
        if (partyId == Guid.Empty)
            throw new ArgumentException("Party id is required.", nameof(partyId));
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role));
        PartyId = partyId;
        Role = role;
    }

    public Guid PartyId { get; private set; }
    public ContractPartyRole Role { get; private set; }
}
