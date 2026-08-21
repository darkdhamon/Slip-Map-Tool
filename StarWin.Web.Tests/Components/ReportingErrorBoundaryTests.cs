using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Web.Components;

namespace StarWin.Web.Tests.Components;

public sealed class ReportingErrorBoundaryTests : BunitContext
{
    [Fact]
    public void Reports_unhandled_interactive_component_exceptions()
    {
        var reporter = new FakeExceptionReporter();
        Services.AddSingleton<IStarWinExceptionReporter>(reporter);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StarforgedAtlas:HostKind"] = "Desktop",
                ["StarforgedAtlas:AppVersion"] = "2026-08-20.0-developer-preview"
            })
            .Build());

        var cut = Render<ReportingErrorBoundary>(parameters => parameters
            .AddChildContent<ThrowingComponent>());

        cut.WaitForAssertion(() =>
        {
            var report = Assert.Single(reporter.Reports);
            Assert.Equal("Desktop", report.Context.HostKind);
            Assert.Equal("Unhandled interactive component", report.Context.Operation);
            Assert.Equal("2026-08-20.0-developer-preview", report.Context.AppVersion);
            Assert.IsType<InvalidOperationException>(report.Exception);
        });

        Assert.Contains("Reload the page", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Omits_query_and_fragment_from_reported_route()
    {
        var reporter = new FakeExceptionReporter();
        Services.AddSingleton<IStarWinExceptionReporter>(reporter);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/systems?authorization_code=secret#details");

        var cut = Render<ReportingErrorBoundary>(parameters => parameters
            .AddChildContent<ThrowingComponent>());

        cut.WaitForAssertion(() =>
            Assert.Equal("/systems", Assert.Single(reporter.Reports).Context.Route));
    }

    private sealed class ThrowingComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            throw new InvalidOperationException("Interactive failure");
        }
    }

    private sealed class FakeExceptionReporter : IStarWinExceptionReporter
    {
        public List<(Exception Exception, StarWinExceptionContext Context)> Reports { get; } = [];

        public Task ReportExceptionAsync(
            Exception exception,
            StarWinExceptionContext context,
            CancellationToken cancellationToken = default)
        {
            Reports.Add((exception, context));
            return Task.CompletedTask;
        }
    }
}
