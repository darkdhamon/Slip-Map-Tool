using System.IO;

namespace StarWin.Web.Tests.Components;

public sealed class AppCssTests
{
    [Fact]
    public void WorkspaceLoadingModalHostStretchesAcrossOverviewRow()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var cssPath = Path.Combine(repoRoot, "StarWin.Web", "wwwroot", "app.css");
        var css = File.ReadAllText(cssPath);

        Assert.Contains(".workspace-loading-modal-host", css);
        Assert.Contains("align-self: stretch;", css);
        Assert.Contains("justify-self: stretch;", css);
    }

    [Fact]
    public void ImportLoadingModalReservesFourLinesOfDetailSpace()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var cssPath = Path.Combine(repoRoot, "StarWin.Web", "wwwroot", "app.css");
        var css = File.ReadAllText(cssPath);

        Assert.Contains(".loading-modal.import-loading-modal .loading-progress-detail", css);
        Assert.Contains("min-height: calc(1.45em * 4);", css);
    }

    [Fact]
    public void HeroOverlayUsesAnIsolatedStackingContext()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var cssPath = Path.Combine(repoRoot, "StarWin.Web", "wwwroot", "app.css");
        var css = File.ReadAllText(cssPath);

        Assert.Contains(".explorer-hero {", css);
        Assert.Contains("isolation: isolate;", css);
        Assert.Contains(".explorer-hero::before,", css);
        Assert.Contains("z-index: 0;", css);
    }

    [Fact]
    public void ExplorerControlStripDefaultsToTabFirstLayoutAndOnlySplitsAtVeryWideBreakpoint()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var cssPath = Path.Combine(repoRoot, "StarWin.Web", "wwwroot", "app.css");
        var css = File.ReadAllText(cssPath);

        Assert.Contains("grid-template-columns: minmax(0, 1fr);", css);
        Assert.Contains(".control-strip-fields {", css);
        Assert.Contains(".control-strip-tabs {", css);
        Assert.Contains("order: -1;", css);
        Assert.Contains("@media (min-width: 1900px) {", css);
        Assert.Contains("grid-template-columns: minmax(0, 760px) minmax(0, 1fr);", css);
        Assert.Contains("order: 0;", css);
    }

    [Fact]
    public void NavigationFocusTargetsMainContentInsteadOfHeading()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var routesPath = Path.Combine(repoRoot, "StarWin.Web", "Components", "Routes.razor");
        var layoutPath = Path.Combine(repoRoot, "StarWin.Web", "Components", "Layout", "MainLayout.razor");
        var layoutCssPath = Path.Combine(repoRoot, "StarWin.Web", "Components", "Layout", "MainLayout.razor.css");
        var routesMarkup = File.ReadAllText(routesPath);
        var layoutMarkup = File.ReadAllText(layoutPath);
        var layoutCss = File.ReadAllText(layoutCssPath);

        Assert.Contains("FocusOnNavigate RouteData=\"routeData\" Selector=\".site-main\"", routesMarkup);
        Assert.Contains("<main class=\"site-main\" tabindex=\"-1\">", layoutMarkup);
        Assert.Contains(".site-main:focus,", layoutCss);
        Assert.Contains("outline: none;", layoutCss);
    }
}
