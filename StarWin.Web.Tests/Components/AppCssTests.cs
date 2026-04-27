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
}
