using Bunit;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class NotFoundPageTests : BunitContext
{
    [Fact]
    public void RendersNotFoundMessage()
    {
        var cut = Render<NotFound>();

        Assert.Contains("Not Found", cut.Markup);
        Assert.Contains("does not exist", cut.Markup);
    }
}
