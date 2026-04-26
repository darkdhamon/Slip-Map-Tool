using Bunit;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Tests.Components;

public sealed class ExplorerSectionTabsTests : BunitContext
{
    [Fact]
    public void RendersLinksAndMarksActiveSection()
    {
        var cut = Render<ExplorerSectionTabs>(parameters => parameters
            .Add(component => component.Sections, ["Overview", "Timeline", "Empires"])
            .Add(component => component.ActiveSection, "Timeline")
            .Add(component => component.SectionHrefFactory, section => $"/sector-explorer/{section.ToLowerInvariant()}"));

        var links = cut.FindAll("a");

        Assert.Collection(
            links,
            link =>
            {
                Assert.Equal("Overview", link.TextContent.Trim());
                Assert.Equal("/sector-explorer/overview", link.GetAttribute("href"));
                Assert.Null(link.GetAttribute("aria-current"));
            },
            link =>
            {
                Assert.Equal("Timeline", link.TextContent.Trim());
                Assert.Equal("/sector-explorer/timeline", link.GetAttribute("href"));
                Assert.Equal("page", link.GetAttribute("aria-current"));
                Assert.Contains("active", link.ClassName);
            },
            link =>
            {
                Assert.Equal("Empires", link.TextContent.Trim());
                Assert.Equal("/sector-explorer/empires", link.GetAttribute("href"));
                Assert.Null(link.GetAttribute("aria-current"));
            });
    }
}
