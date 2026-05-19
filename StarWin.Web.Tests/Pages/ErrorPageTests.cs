using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class ErrorPageTests : BunitContext
{
    [Fact]
    public void ErrorPage_reports_the_exception_handler_feature_context()
    {
        var reporter = new FakeExceptionReporter();
        Services.AddSingleton<IStarWinExceptionReporter>(reporter);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StarforgedAtlas:HostKind"] = "Desktop"
            })
            .Build());

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123",
            RequestServices = Services
        };
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/systems";
        httpContext.Request.QueryString = new QueryString("?focus=1");
        httpContext.Features.Set<IExceptionHandlerPathFeature>(new ExceptionHandlerFeature
        {
            Error = new InvalidOperationException("Boom"),
            Path = "/systems"
        });

        var cut = Render<CascadingValue<HttpContext>>(parameters => parameters
            .Add(component => component.Value, httpContext)
            .AddChildContent<Error>());

        cut.WaitForAssertion(() =>
        {
            var report = Assert.Single(reporter.Reports);
            Assert.Equal("Desktop", report.Context.HostKind);
            Assert.Equal("/systems", report.Context.Route);
            Assert.Equal("trace-123", report.Context.TraceIdentifier);
            Assert.Contains("GET", report.Context.AdditionalData!["Request method"], StringComparison.Ordinal);
        });

        Assert.Contains("trace-123", cut.Markup);
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
