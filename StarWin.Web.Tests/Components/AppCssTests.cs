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
    public void NavigationFocusTargetsMainContentInsteadOfHeading()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var routesPath = Path.Combine(repoRoot, "StarWin.Web", "Components", "Routes.razor");
        var layoutPath = Path.Combine(repoRoot, "StarWin.Web", "Components", "Layout", "MainLayout.razor");
        var routesMarkup = File.ReadAllText(routesPath);
        var layoutMarkup = File.ReadAllText(layoutPath);

        Assert.Contains("FocusOnNavigate RouteData=\"routeData\" Selector=\".site-main\"", routesMarkup);
        Assert.Contains("<main class=\"site-main\" tabindex=\"-1\">", layoutMarkup);
    }
}
