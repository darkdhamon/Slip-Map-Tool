using Bunit;
using StarWin.Web.Components.Explorer;

namespace StarWin.Web.Tests.Components;

public sealed class ExplorerMetricsBandTests : BunitContext
{
    [Fact]
    public void RendersAllSuppliedCounts()
    {
        var cut = Render<ExplorerMetricsBand>(parameters => parameters
            .Add(component => component.SystemCount, 10)
            .Add(component => component.WorldCount, 20)
            .Add(component => component.ColonyCount, 30)
            .Add(component => component.EmpireCount, 40)
            .Add(component => component.RaceCount, 50));

        cut.MarkupMatches("""
        <section class="metrics-band" aria-label="sector summary">
          <article><span>Systems</span><strong>10</strong></article>
          <article><span>Worlds</span><strong>20</strong></article>
          <article><span>Colonies</span><strong>30</strong></article>
          <article><span>Empires</span><strong>40</strong></article>
          <article><span>Races</span><strong>50</strong></article>
        </section>
        """);
    }
}
