using NotaireDesk.Domain.Properties;

namespace NotaireDesk.Domain.Contracts;

/// <summary>Immutable-at-the-domain-layer property copy for a contract generation.</summary>
public sealed class PropertyGenerationSnapshot
{
    private PropertyGenerationSnapshot() { }

    internal PropertyGenerationSnapshot(
        Guid sourcePropertyId,
        PropertyKind kind,
        string descriptionAr,
        string? descriptionFr,
        string wilaya,
        string commune,
        decimal? areaSquareMeters,
        string? landRegistryReference)
    {
        SourcePropertyId = sourcePropertyId;
        Kind = kind;
        DescriptionAr = descriptionAr;
        DescriptionFr = descriptionFr;
        Wilaya = wilaya;
        Commune = commune;
        AreaSquareMeters = areaSquareMeters;
        LandRegistryReference = landRegistryReference;
    }

    public Guid SourcePropertyId { get; private set; }
    public PropertyKind Kind { get; private set; }
    public string DescriptionAr { get; private set; } = string.Empty;
    public string? DescriptionFr { get; private set; }
    public string Wilaya { get; private set; } = string.Empty;
    public string Commune { get; private set; } = string.Empty;
    public decimal? AreaSquareMeters { get; private set; }
    public string? LandRegistryReference { get; private set; }
}
