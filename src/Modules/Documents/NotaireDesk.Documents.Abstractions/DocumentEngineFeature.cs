namespace NotaireDesk.Documents;

/// <summary>
/// Feature claims used to describe candidates. Claims are not proof of fidelity:
/// every selected feature must pass the conformance fixtures in docs/fixtures.
/// </summary>
public enum DocumentEngineFeature
{
    InteractiveEditing,
    ArabicRightToLeftParagraphs,
    MixedArabicFrenchTextRuns,
    FootnotesAndEndnotes,
    HeadersAndFooters,
    ComplexTables,
    InlineAndAnchoredImages,
    PageLayoutAndPagination,
    SectionBreaks,
    MailMerge,
    CommentsAndTrackChanges,
    FormFields
}
