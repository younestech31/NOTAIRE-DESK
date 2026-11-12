using NotaireDesk.Documents;
using Xunit;

namespace NotaireDesk.Tests;

public sealed class DocumentEngineDescriptorTests
{
    [Fact]
    public void Constructor_TrimsIdentityFields()
    {
        var descriptor = CreateDescriptor();

        Assert.Equal("engine.test", descriptor.Id);
        Assert.Equal("Test Engine", descriptor.DisplayName);
        Assert.Equal("Example Vendor", descriptor.Vendor);
        Assert.Equal("1.0", descriptor.Version);
    }

    [Fact]
    public void ImportAndExportCapabilities_AreIndependent()
    {
        var descriptor = CreateDescriptor();

        Assert.True(descriptor.CanImport(DocumentFileFormat.Doc));
        Assert.True(descriptor.CanImport(DocumentFileFormat.Docx));
        Assert.False(descriptor.CanImport(DocumentFileFormat.Pdf));
        Assert.True(descriptor.CanExport(DocumentFileFormat.Docx));
        Assert.True(descriptor.CanExport(DocumentFileFormat.Pdf));
        Assert.False(descriptor.CanExport(DocumentFileFormat.Doc));
    }

    [Fact]
    public void Features_AreReportedWithoutAssumingUnlistedCapabilities()
    {
        var descriptor = CreateDescriptor();

        Assert.True(descriptor.Supports(DocumentEngineFeature.ArabicRightToLeftParagraphs));
        Assert.True(descriptor.Supports(DocumentEngineFeature.FootnotesAndEndnotes));
        Assert.False(descriptor.Supports(DocumentEngineFeature.MailMerge));
    }

    [Fact]
    public void Constructor_RejectsBlankEngineId()
    {
        Assert.Throws<ArgumentException>(() => new DocumentEngineDescriptor(
            " ", "Name", "Vendor", "1.0",
            [DocumentFileFormat.Docx],
            [DocumentFileFormat.Docx],
            []));
    }

    private static DocumentEngineDescriptor CreateDescriptor() => new(
        " engine.test ",
        " Test Engine ",
        " Example Vendor ",
        " 1.0 ",
        [DocumentFileFormat.Doc, DocumentFileFormat.Docx],
        [DocumentFileFormat.Docx, DocumentFileFormat.Pdf],
        [
            DocumentEngineFeature.ArabicRightToLeftParagraphs,
            DocumentEngineFeature.FootnotesAndEndnotes
        ]);
}
