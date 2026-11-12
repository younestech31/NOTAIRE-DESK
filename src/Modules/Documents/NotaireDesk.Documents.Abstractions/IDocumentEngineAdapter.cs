namespace NotaireDesk.Documents;

/// <summary>
/// Adapter boundary for a selected document engine. An adapter must not persist
/// contract data to cloud services or silently call external conversion services.
/// </summary>
public interface IDocumentEngineAdapter
{
    DocumentEngineDescriptor Descriptor { get; }

    ValueTask<IDocumentEngineSession> CreateSessionAsync(
        CancellationToken cancellationToken = default);
}
