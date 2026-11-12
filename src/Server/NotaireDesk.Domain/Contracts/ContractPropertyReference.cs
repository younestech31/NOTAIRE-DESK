namespace NotaireDesk.Domain.Contracts;

/// <summary>Links a property master record to a contract.</summary>
public sealed class ContractPropertyReference
{
    private ContractPropertyReference() { }

    internal ContractPropertyReference(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            throw new ArgumentException("Property id is required.", nameof(propertyId));
        PropertyId = propertyId;
    }

    public Guid PropertyId { get; private set; }
}
