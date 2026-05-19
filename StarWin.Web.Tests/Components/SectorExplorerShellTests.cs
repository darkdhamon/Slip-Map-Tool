using Bunit;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Tests.Components;

public sealed class SectorExplorerShellTests : BunitContext
{
    [Fact]
    public void GroupsTabsSeparatelyFromExplorerSelectorsAndSearch()
    {
        var sector = new StarWinSector
        {
            Id = 7,
            Name = "Del Corra"
        };
        var system = new StarSystem
        {
            Id = 11,
            SectorId = sector.Id,
            Name = "Helios"
        };
        sector.Systems.Add(system);

        var cut = Render<SectorExplorerShell>(parameters => parameters
            .Add(component => component.Sectors, [sector])
            .Add(component => component.Systems, [system])
            .Add(component => component.SelectedSectorId, sector.Id)
            .Add(component => component.SelectedSystemText, "11 - Helios")
            .Add(component => component.Sections, ["Overview", "Systems"])
            .Add(component => component.ActiveSection, "Overview")
            .Add(component => component.SectionHrefFactory, section => $"/sector-explorer/{section.ToLowerInvariant()}"));

        var controlStrip = cut.Find(".control-strip");
        var fieldGroup = cut.Find(".control-strip-fields");
        var tabGroup = cut.Find(".control-strip-tabs");

        Assert.Contains("control-strip-tabs", controlStrip.Children[0].ClassName);
        Assert.Contains("control-strip-fields", controlStrip.Children[1].ClassName);
        Assert.Single(fieldGroup.GetElementsByClassName("sector-field"));
        Assert.Single(fieldGroup.GetElementsByClassName("system-field"));
        Assert.Single(fieldGroup.GetElementsByClassName("archive-search"));

        var sectionLinks = tabGroup.QuerySelectorAll(".section-tabs a")
            .Select(link => link.TextContent.Trim())
            .ToArray();

        Assert.Equal(["Overview", "Systems"], sectionLinks);
    }
}
