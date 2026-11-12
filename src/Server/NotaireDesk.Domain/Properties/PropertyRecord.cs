namespace NotaireDesk.Domain.Properties;

/// <summary>Mutable reference data for a property within one office database.</summary>
public sealed class PropertyRecord
{
    private PropertyRecord() { }

    private PropertyRecord(
        Guid id,
        PropertyKind kind,
        string descriptionAr,
        string? descriptionFr,
        string wilaya,
        string commune,
        decimal? areaSquareMeters,
        string? landRegistryReference,
        DateTimeOffset createdAt)
    {
        Id = id;
        Kind = kind;
        DescriptionAr = descriptionAr;
        DescriptionFr = descriptionFr;
        Wilaya = wilaya;
        Commune = commune;
        AreaSquareMeters = areaSquareMeters;
        LandRegistryReference = landRegistryReference;
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public PropertyKind Kind { get; private set; }
    public string DescriptionAr { get; private set; } = string.Empty;
    public string? DescriptionFr { get; private set; }
    public string Wilaya { get; private set; } = string.Empty;
    public string Commune { get; private set; } = string.Empty;
    public decimal? AreaSquareMeters { get; private set; }
    public string? LandRegistryReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PropertyRecord Create(
        PropertyKind kind,
        string descriptionAr,
        string? descriptionFr,
        string wilaya,
        string commune,
        decimal? areaSquareMeters,
        string? landRegistryReference,
        DateTimeOffset createdAt)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(wilaya);
        ArgumentException.ThrowIfNullOrWhiteSpace(commune);
        ValidateArea(areaSquareMeters);

        return new PropertyRecord(
            Guid.NewGuid(), kind, descriptionAr.Trim(), NormalizeOptional(descriptionFr),
            wilaya.Trim(), commune.Trim(), areaSquareMeters,
            NormalizeOptional(landRegistryReference), createdAt);
    }

    public void UpdateDescription(
        string descriptionAr,
        string? descriptionFr,
        string wilaya,
        string commune,
        decimal? areaSquareMeters,
        string? landRegistryReference,
        DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(wilaya);
        ArgumentException.ThrowIfNullOrWhiteSpace(commune);
        ValidateArea(areaSquareMeters);

        var timestamp = updatedAt.ToUniversalTime();
        if (timestamp < CreatedAt)
            throw new ArgumentOutOfRangeException(nameof(updatedAt), "Update time cannot precede creation time.");

        DescriptionAr = descriptionAr.Trim();
        DescriptionFr = NormalizeOptional(descriptionFr);
        Wilaya = wilaya.Trim();
        Commune = commune.Trim();
        AreaSquareMeters = areaSquareMeters;
        LandRegistryReference = NormalizeOptional(landRegistryReference);
        UpdatedAt = timestamp;
    }

    private static void ValidateArea(decimal? area)
    {
        if (area is < 0)
            throw new ArgumentOutOfRangeException(nameof(area), "Area cannot be negative.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
