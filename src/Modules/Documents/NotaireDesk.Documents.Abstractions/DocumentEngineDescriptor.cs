namespace NotaireDesk.Documents;

/// <summary>Vendor-reported capabilities for a named engine/version under evaluation.</summary>
public sealed class DocumentEngineDescriptor
{
    private readonly HashSet<DocumentFileFormat> _importFormats;
    private readonly HashSet<DocumentFileFormat> _exportFormats;
    private readonly HashSet<DocumentEngineFeature> _features;

    public DocumentEngineDescriptor(
        string id,
        string displayName,
        string vendor,
        string version,
        IEnumerable<DocumentFileFormat> importFormats,
        IEnumerable<DocumentFileFormat> exportFormats,
        IEnumerable<DocumentEngineFeature> features)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(vendor);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(importFormats);
        ArgumentNullException.ThrowIfNull(exportFormats);
        ArgumentNullException.ThrowIfNull(features);

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Vendor = vendor.Trim();
        Version = version.Trim();
        _importFormats = new HashSet<DocumentFileFormat>(importFormats);
        _exportFormats = new HashSet<DocumentFileFormat>(exportFormats);
        _features = new HashSet<DocumentEngineFeature>(features);
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Vendor { get; }
    public string Version { get; }

    public IReadOnlySet<DocumentFileFormat> ImportFormats => _importFormats;
    public IReadOnlySet<DocumentFileFormat> ExportFormats => _exportFormats;
    public IReadOnlySet<DocumentEngineFeature> Features => _features;

    public bool CanImport(DocumentFileFormat format) => _importFormats.Contains(format);
    public bool CanExport(DocumentFileFormat format) => _exportFormats.Contains(format);
    public bool Supports(DocumentEngineFeature feature) => _features.Contains(feature);
}
