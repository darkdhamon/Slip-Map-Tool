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

    [Fact]
    public void AstralBodyRadiusBaselineIsDoubled()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(repoRoot, "StarWin.Web", "wwwroot", "js", "sectorMap3d.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("return selected ? 1.44 : 1.08;", script);
        Assert.Contains("const radius = (0.34 + Math.min(0.76, Math.max(0.18, scale) * 0.34)) * 2;", script);
    }

    [Fact]
    public void KeyboardNavigationTracksHeldKeysAndCancelsConflicts()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(repoRoot, "StarWin.Web", "wwwroot", "js", "sectorMap3d.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("activeKeys: new Set()", script);
        Assert.Contains("canvas.addEventListener(\"keyup\", event => handleKeyUp(state, event));", script);
        Assert.Contains("canvas.addEventListener(\"blur\", () => clearActiveKeys(state));", script);
        Assert.Contains("const yawDirection = getAxisDirection(state.activeKeys, [\"a\"], [\"d\"]);", script);
        Assert.Contains("const pitchDirection = getAxisDirection(state.activeKeys, [\"w\"], [\"s\"]);", script);
        Assert.Contains("const rollDirection = getAxisDirection(state.activeKeys, [\"e\"], [\"q\"]);", script);
        Assert.Contains("if (negativeActive === positiveActive) {", script);
    }

    [Fact]
    public void KeyboardNavigationSupportsZoomAndArrowKeyPanning()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(repoRoot, "StarWin.Web", "wwwroot", "js", "sectorMap3d.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("case \"add\":", script);
        Assert.Contains("case \"subtract\":", script);
        Assert.Contains("const zoomDirection = getAxisDirection(state.activeKeys, [\"-\"], [\"+\"]);", script);
        Assert.Contains("const panHorizontalDirection = getAxisDirection(state.activeKeys, [\"arrowleft\"], [\"arrowright\"]);", script);
        Assert.Contains("const panVerticalDirection = getAxisDirection(state.activeKeys, [\"arrowdown\"], [\"arrowup\"]);", script);
        Assert.Contains("state.radius = Math.max(minimumCameraRadius, Math.min(maximumCameraRadius, state.radius + radiusDelta));", script);
        Assert.Contains("state.target.addScaledVector(right, horizontalDelta);", script);
        Assert.Contains("state.target.addScaledVector(up, verticalDelta);", script);
    }
}
