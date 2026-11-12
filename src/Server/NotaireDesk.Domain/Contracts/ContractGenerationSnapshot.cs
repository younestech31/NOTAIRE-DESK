namespace NotaireDesk.Domain.Contracts;

/// <summary>
/// Captures the exact source data used to produce one document revision. This is
/// append-only through the domain API: later edits to master records cannot alter
/// existing snapshots.
/// </summary>
public sealed class ContractGenerationSnapshot
{
    private readonly List<PartyGenerationSnapshot> _parties = [];
    private readonly List<PropertyGenerationSnapshot> _properties = [];

    private ContractGenerationSnapshot() { }

    internal ContractGenerationSnapshot(
        Guid id,
        Guid contractId,
        Guid templateId,
        int templateVersion,
        int generationNumber,
        Guid generatedBy,
        DateTimeOffset generatedAt,
        IEnumerable<PartyGenerationSnapshot> parties,
        IEnumerable<PropertyGenerationSnapshot> properties)
    {
        Id = id;
        ContractId = contractId;
        TemplateId = templateId;
        TemplateVersion = templateVersion;
        GenerationNumber = generationNumber;
        GeneratedBy = generatedBy;
        GeneratedAt = generatedAt.ToUniversalTime();
        _parties.AddRange(parties);
        _properties.AddRange(properties);
    }

    public Guid Id { get; private set; }
    public Guid ContractId { get; private set; }
    public Guid TemplateId { get; private set; }
    public int TemplateVersion { get; private set; }
    public int GenerationNumber { get; private set; }
    public Guid GeneratedBy { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public IReadOnlyCollection<PartyGenerationSnapshot> Parties => _parties.AsReadOnly();
    public IReadOnlyCollection<PropertyGenerationSnapshot> Properties => _properties.AsReadOnly();
}
