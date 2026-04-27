using Bunit;
using Microsoft.AspNetCore.Components;
using StarWin.Web.Components;

namespace StarWin.Web.Tests.Components;

public sealed class LoadingModalTests : BunitContext
{
    [Fact]
    public void RendersSharedLoadingModalContentWhenVisible()
    {
        var cut = Render<LoadingModal>(parameters => parameters
            .Add(component => component.IsVisible, true)
            .Add(component => component.PanelClass, "import-loading-modal")
            .Add(component => component.AriaLabel, "Loading Timeline")
            .Add(component => component.Kicker, "Loading Timeline")
            .Add(component => component.Title, "Preparing timeline events for the selected sector.")
            .Add(component => component.Detail, "The page will stay visible until the first chronology batch is ready.")
            .Add(component => component.StartedAtUnixMs, 123456789L)
            .Add(component => component.Steps, new[]
            {
                "Resolve the selected sector context.",
                "Load the first timeline event page.",
                "Render the chronology and detail workspace."
            }));

        Assert.Contains("Loading Timeline", cut.Markup);
        Assert.Contains("Preparing timeline events for the selected sector.", cut.Markup);
        Assert.Contains("The page will stay visible until the first chronology batch is ready.", cut.Markup);
        Assert.Contains("loading-modal import-loading-modal", cut.Markup);
        Assert.Contains("data-loading-timer=\"true\"", cut.Markup);
        Assert.Contains("data-started-at-unix-ms=\"123456789\"", cut.Markup);
        Assert.Equal(3, cut.FindAll("ol li").Count);
    }

    [Fact]
    public void KeepsGlobalOverlayInDomWhenAlwaysRenderIsEnabled()
    {
        var cut = Render<LoadingModal>(parameters => parameters
            .Add(component => component.IsVisible, false)
            .Add(component => component.AlwaysRender, true)
            .Add(component => component.OverlayId, "explorer-section-loading")
            .Add(component => component.AriaLabel, "Loading section")
            .Add(component => component.KickerContent, (RenderFragment)(builder =>
            {
                builder.AddMarkupContent(0, "<span class=\"panel-kicker\" data-section-loading-target>Loading section</span>");
            }))
            .Add(component => component.TitleContent, (RenderFragment)(builder =>
            {
                builder.AddMarkupContent(0, "<h2 data-section-loading-status>Preparing explorer records.</h2>");
            })));

        var overlay = cut.Find("#explorer-section-loading");
        Assert.NotNull(overlay);
        Assert.True(overlay.HasAttribute("hidden"));
        Assert.Contains("data-section-loading-target", cut.Markup);
        Assert.Contains("data-section-loading-status", cut.Markup);
        Assert.Contains("data-loading-timer=\"true\"", cut.Markup);
        Assert.Contains("Elapsed time:", cut.Markup);
    }
}
