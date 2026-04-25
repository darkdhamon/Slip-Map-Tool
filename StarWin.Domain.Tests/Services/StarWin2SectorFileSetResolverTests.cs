using StarWin.Domain.Services;

namespace StarWin.Domain.Tests.Services;

public sealed class StarWin2SectorFileSetResolverTests
{
    [Fact]
    public void Resolve_ReturnsBasePathAndSectorName()
    {
        var resolver = new StarWin2SectorFileSetResolver();
        var sectorFilePath = Path.Combine("C:\\atlas", "imports", "Delcora.sec");

        var result = resolver.Resolve(sectorFilePath);

        Assert.Equal(Path.Combine("C:\\atlas", "imports"), result.BasePath);
        Assert.Equal("Delcora", result.SectorName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Resolve_ThrowsForBlankPath(string sectorFilePath)
    {
        var resolver = new StarWin2SectorFileSetResolver();

        var exception = Assert.Throws<ArgumentException>(() => resolver.Resolve(sectorFilePath));

        Assert.Equal("sectorFilePath", exception.ParamName);
    }
}
