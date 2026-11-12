using NotaireDesk.Domain;
using Xunit;

namespace NotaireDesk.Tests;
public sealed class OfficeIdentityTests
{
    [Fact]
    public void Create_TrimsName_AndGeneratesId()
    {
        var office = OfficeIdentity.Create("  مكتب تجريبي  ");
        Assert.Equal("مكتب تجريبي", office.DisplayName);
        Assert.NotEqual(Guid.Empty, office.Id);
    }

    [Fact]
    public void Create_RejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => OfficeIdentity.Create(" "));
    }
}
