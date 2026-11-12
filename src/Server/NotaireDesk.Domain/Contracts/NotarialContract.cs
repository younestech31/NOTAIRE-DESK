using NotaireDesk.Domain.Parties;
using NotaireDesk.Domain.Properties;

namespace NotaireDesk.Domain.Contracts;

/// <summary>
/// A notarial instrument being prepared by one dedicated office database.
/// It deliberately carries no caller-supplied tenant identifier.
/// </summary>
public sealed class NotarialContract
{
    private readonly List<ContractPartyReference> _parties = [];
    private readonly List<ContractPropertyReference> _properties = [];
    private readonly List<ContractGenerationSnapshot> _generations = [];

    private NotarialContract() { }

    private NotarialContract(Guid id, string reference, string instrumentType, Guid createdBy, DateTimeOffset createdAt)
    {
        Id = id;
        Reference = reference;
        InstrumentType = instrumentType;
        CreatedBy = createdBy;
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
        Status = ContractStatus.Draft;
    }

    public Guid Id { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public string InstrumentType { get; private set; } = string.Empty;
    public ContractStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public IReadOnlyCollection<ContractPartyReference> Parties => _parties.AsReadOnly();
    public IReadOnlyCollection<ContractPropertyReference> Properties => _properties.AsReadOnly();
    public IReadOnlyCollection<ContractGenerationSnapshot> Generations => _generations.AsReadOnly();

    public static NotarialContract Create(string reference, string instrumentType, Guid createdBy, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(instrumentType);
        if (createdBy == Guid.Empty)
            throw new ArgumentException("Creator id is required.", nameof(createdBy));

        return new NotarialContract(Guid.NewGuid(), reference.Trim(), instrumentType.Trim(), createdBy, createdAt);
    }

    public void AttachParty(Guid partyId, ContractPartyRole role, DateTimeOffset changedAt)
    {
        EnsureDraft();
        if (_parties.Any(x => x.PartyId == partyId && x.Role == role))
            throw new InvalidOperationException("This party is already attached with the selected role.");

        var timestamp = ValidateChangeTime(changedAt);
        _parties.Add(new ContractPartyReference(partyId, role));
        UpdatedAt = timestamp;
    }

    public void AttachProperty(Guid propertyId, DateTimeOffset changedAt)
    {
        EnsureDraft();
        if (_properties.Any(x => x.PropertyId == propertyId))
            throw new InvalidOperationException("This property is already attached to the contract.");

        var timestamp = ValidateChangeTime(changedAt);
        _properties.Add(new ContractPropertyReference(propertyId));
        UpdatedAt = timestamp;
    }

    public ContractGenerationSnapshot CreateGenerationSnapshot(
        Guid templateId,
        int templateVersion,
        Guid generatedBy,
        DateTimeOffset generatedAt,
        IEnumerable<Party> parties,
        IEnumerable<PropertyRecord> properties)
    {
        EnsureDraft();

        if (templateId == Guid.Empty)
            throw new ArgumentException("Template id is required.", nameof(templateId));
        if (templateVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(templateVersion));
        if (generatedBy == Guid.Empty)
            throw new ArgumentException("Generator id is required.", nameof(generatedBy));
        ArgumentNullException.ThrowIfNull(parties);
        ArgumentNullException.ThrowIfNull(properties);

        var timestamp = ValidateChangeTime(generatedAt);
        var partyMap = parties.ToDictionary(x => x.Id);
        var propertyMap = properties.ToDictionary(x => x.Id);

        // A single person may occupy multiple roles (for example, buyer and
        // representative). The master record is supplied once, then snapshotted
        // once for each distinct contract role.
        var expectedPartyIds = _parties.Select(x => x.PartyId).Distinct().ToArray();
        if (partyMap.Count != expectedPartyIds.Length ||
            expectedPartyIds.Any(id => !partyMap.ContainsKey(id)))
        {
            throw new InvalidOperationException(
                "The generation snapshot must include every linked party exactly once.");
        }

        if (propertyMap.Count != _properties.Count ||
            _properties.Any(x => !propertyMap.ContainsKey(x.PropertyId)))
        {
            throw new InvalidOperationException(
                "The generation snapshot must include every linked property exactly once.");
        }

        var partySnapshots = _parties.Select(link =>
        {
            var party = partyMap[link.PartyId];
            return new PartyGenerationSnapshot(
                party.Id, link.Role, party.Kind, party.DisplayNameAr, party.DisplayNameFr);
        }).ToArray();

        var propertySnapshots = _properties.Select(link =>
        {
            var property = propertyMap[link.PropertyId];
            return new PropertyGenerationSnapshot(
                property.Id,
                property.Kind,
                property.DescriptionAr,
                property.DescriptionFr,
                property.Wilaya,
                property.Commune,
                property.AreaSquareMeters,
                property.LandRegistryReference);
        }).ToArray();

        var snapshot = new ContractGenerationSnapshot(
            Guid.NewGuid(),
            Id,
            templateId,
            templateVersion,
            _generations.Count + 1,
            generatedBy,
            timestamp,
            partySnapshots,
            propertySnapshots);

        UpdatedAt = timestamp;
        _generations.Add(snapshot);
        return snapshot;
    }

    public void SubmitForReview(DateTimeOffset changedAt)
    {
        EnsureStatus(ContractStatus.Draft);
        ChangeStatus(ContractStatus.UnderReview, changedAt);
    }

    public void ReturnToDraft(DateTimeOffset changedAt)
    {
        EnsureStatus(ContractStatus.UnderReview);
        ChangeStatus(ContractStatus.Draft, changedAt);
    }

    public void Approve(Guid notaryUserId, DateTimeOffset approvedAt)
    {
        EnsureStatus(ContractStatus.UnderReview);
        if (notaryUserId == Guid.Empty)
            throw new ArgumentException("Approving notary id is required.", nameof(notaryUserId));

        var timestamp = ValidateChangeTime(approvedAt);
        ChangeStatus(ContractStatus.Approved, timestamp);
        ApprovedBy = notaryUserId;
        ApprovedAt = timestamp;
    }

    public void MarkExecuted(DateTimeOffset executedAt)
    {
        EnsureStatus(ContractStatus.Approved);
        var timestamp = ValidateChangeTime(executedAt);
        ChangeStatus(ContractStatus.Executed, timestamp);
        ExecutedAt = timestamp;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status is not (ContractStatus.Executed or ContractStatus.Draft))
            throw new InvalidOperationException("Only draft or executed contracts can be archived.");

        ChangeStatus(ContractStatus.Archived, archivedAt);
    }

    private void EnsureDraft()
    {
        if (Status != ContractStatus.Draft)
            throw new InvalidOperationException("Return the contract to draft before editing or generating it.");
    }

    private void EnsureStatus(ContractStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Invalid status transition from {Status}.");
    }

    private void ChangeStatus(ContractStatus status, DateTimeOffset changedAt)
    {
        var timestamp = ValidateChangeTime(changedAt);
        Status = status;
        UpdatedAt = timestamp;
    }

    private DateTimeOffset ValidateChangeTime(DateTimeOffset changedAt)
    {
        var timestamp = changedAt.ToUniversalTime();
        if (timestamp < CreatedAt)
            throw new ArgumentOutOfRangeException(nameof(changedAt), "Change time cannot precede creation time.");
        return timestamp;
    }
}
