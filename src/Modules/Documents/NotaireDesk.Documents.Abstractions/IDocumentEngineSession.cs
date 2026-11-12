namespace NotaireDesk.Documents;

/// <summary>
/// One isolated editing/conversion session. Implementations own their native
/// editor/document model and must dispose engine resources when the session closes.
/// </summary>
public interface IDocumentEngineSession : IAsyncDisposable
{
    bool IsDocumentLoaded { get; }

    ValueTask OpenAsync(
        Stream source,
        DocumentFileFormat format,
        CancellationToken cancellationToken = default);

    ValueTask SaveAsync(
        Stream destination,
        DocumentFileFormat format,
        CancellationToken cancellationToken = default);

    ValueTask<int> GetPageCountAsync(CancellationToken cancellationToken = default);
}
