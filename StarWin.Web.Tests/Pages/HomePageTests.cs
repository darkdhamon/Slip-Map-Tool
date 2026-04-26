using Bunit;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void RendersExplorerSplitMessageAndCta()
    {
        var cut = Render<Home>();

        Assert.Contains("Sector Explorer has moved", cut.Markup);
        Assert.Contains("Open Sector Explorer", cut.Markup);
        Assert.Contains("href=\"/sector-explorer\"", cut.Markup);
    }
}
