using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Web.Components.Layout;

namespace StarWin.Web.Tests.Components;

public sealed class SectorExplorerLayoutSyncTests : BunitContext
{
    [Fact]
    public void SyncComponentPublishesLayoutStateToStore()
    {
        Services.AddScoped<SectorExplorerLayoutStateStore>();
        var state = new SectorExplorerLayoutState(
            null,
            [],
            [],
            7,
            default,
            "11 - Helios",
            default,
            "hel",
            default,
            [],
            default,
            ["Overview", "Timeline"],
            "Overview",
            string.Empty,
            null,
            null);

        Render<SectorExplorerLayoutSync>(parameters => parameters.Add(component => component.State, state));

        var store = Services.GetRequiredService<SectorExplorerLayoutStateStore>();
        Assert.Same(state, store.State);
    }
}
