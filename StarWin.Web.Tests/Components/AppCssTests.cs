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
}
