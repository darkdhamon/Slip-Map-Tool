using StarWin.Domain.Services;

namespace StarWin.Domain.Tests.Services;

public sealed class SectorRoutePlannerTests
{
    [Fact]
    public void BuildHyperlaneNetworkReport_ReportsConnectedAndStrandedSystems()
    {
        var eligibleSystemIds = new[] { 101, 102, 103, 104 };
        var routes = new[]
        {
            new SectorHyperlaneRouteDefinition(
                101,
                102,
                0.5d,
                0.12d,
                9,
                "Prime Hyperlane",
                1,
                "Alderon",
                1,
                "Alderon"),
            new SectorHyperlaneRouteDefinition(
                102,
                103,
                0.75d,
                0.18d,
                7,
                "Enhanced Hyperlane",
                2,
                "Kell",
                2,
                "Kell")
        };

        var report = SectorRoutePlanner.BuildHyperlaneNetworkReport(eligibleSystemIds, routes);

        Assert.Equal(1, report.DistinctNetworkCount);
        Assert.Equal([3], report.NetworkSizes);
        Assert.Equal(1, report.StrandedSystemCount);
        Assert.Equal(4, report.EligibleSystemCount);
        Assert.Equal(3, report.ConnectedSystemCount);
    }
}
