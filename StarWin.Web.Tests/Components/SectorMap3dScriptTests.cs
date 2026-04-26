using System.IO;

namespace StarWin.Web.Tests.Components;

public sealed class SectorMap3dScriptTests
{
    [Fact]
    public void RouteRendererSupportsCurrentSectorMapRoutePayloadNames()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(repoRoot, "StarWin.Web", "wwwroot", "js", "sectorMap3d.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("route.sourceSystemId ?? route.sourceId ?? null", script);
        Assert.Contains("route.targetSystemId ?? route.targetId ?? null", script);
        Assert.Contains("route.technologyLevel ?? route.hyperlaneTechLevel ?? null", script);
    }
}
