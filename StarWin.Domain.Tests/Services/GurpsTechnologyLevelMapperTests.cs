using StarWin.Domain.Services;

namespace StarWin.Domain.Tests.Services;

public sealed class GurpsTechnologyLevelMapperTests
{
    [Fact]
    public void TryParseDisplay_WithSuperscienceValueAndWhitespace_ReturnsExpected()
    {
        var result = GurpsTechnologyLevelMapper.TryParseDisplay(" 14^ ", out var baseTechLevel, out var isSuperscience);

        Assert.True(result);
        Assert.Equal(14, baseTechLevel);
        Assert.True(isSuperscience);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseDisplay_WithNullEmptyOrWhitespaceValue_ReturnsFalse(string? value)
    {
        var result = GurpsTechnologyLevelMapper.TryParseDisplay(value, out var baseTechLevel, out var isSuperscience);

        Assert.False(result);
        Assert.Equal(0, baseTechLevel);
        Assert.False(isSuperscience);
    }
}
