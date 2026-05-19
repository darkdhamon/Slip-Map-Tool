using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Infrastructure;
using StarWin.Infrastructure.Services;
using StarforgedAtlas.GitHubReporting;
using Xunit;

namespace StarWin.Infrastructure.Tests.Diagnostics;

public sealed class StarWinExceptionReporterTests
{
    [Fact]
    public async Task ReportExceptionAsync_publishes_issue_with_runtime_context()
    {
        var publisher = new FakeGitHubIssuePublisher();
        var reporter = new StarWinExceptionReporter(
            publisher,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["StarforgedAtlas:IssueReporting:RepositoryFullName"] = "darkdhamon/Starforged-Atlas",
                    ["StarforgedAtlas:IssueReporting:ProjectOwner"] = "darkdhamon",
                    ["StarforgedAtlas:IssueReporting:ProjectTitle"] = "Starforged Atlas Task Board"
                })
                .Build());

        await reporter.ReportExceptionAsync(
            new InvalidOperationException("Boom"),
            new StarWinExceptionContext(
                HostKind: "Desktop",
                Operation: "Desktop shell startup",
                Route: "/systems",
                RequestId: "request-123",
                TraceIdentifier: "trace-456",
                AppVersion: "2026-05-19.0-developer-preview",
                AdditionalData: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["Local URL"] = "http://127.0.0.1:10103"
                }));

        var submission = Assert.Single(publisher.Submissions);
        Assert.Equal("darkdhamon/Starforged-Atlas", submission.Target.RepositoryFullName);
        Assert.Contains("Desktop InvalidOperationException", submission.Title, StringComparison.Ordinal);
        Assert.Contains("Host kind: `Desktop`", submission.Body, StringComparison.Ordinal);
        Assert.Contains("Operation: `Desktop shell startup`", submission.Body, StringComparison.Ordinal);
        Assert.Contains("Route/page: `/systems`", submission.Body, StringComparison.Ordinal);
        Assert.Contains("Request ID: `request-123`", submission.Body, StringComparison.Ordinal);
        Assert.Contains("Trace identifier: `trace-456`", submission.Body, StringComparison.Ordinal);
        Assert.Contains("App version: `2026-05-19.0-developer-preview`", submission.Body, StringComparison.Ordinal);
        Assert.Contains("Local URL", submission.Body, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException: Boom", submission.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportExceptionAsync_skips_operation_canceled_exceptions()
    {
        var publisher = new FakeGitHubIssuePublisher();
        var reporter = new StarWinExceptionReporter(publisher);

        await reporter.ReportExceptionAsync(
            new OperationCanceledException("Canceled"),
            new StarWinExceptionContext(
                HostKind: "Web",
                Operation: "Unhandled web request"));

        Assert.Empty(publisher.Submissions);
    }

    [Fact]
    public async Task ReportExceptionAsync_suppresses_duplicates_within_the_same_reporter_instance()
    {
        var publisher = new FakeGitHubIssuePublisher();
        var reporter = new StarWinExceptionReporter(publisher);
        var context = new StarWinExceptionContext(
            HostKind: "Web",
            Operation: "Unhandled web request",
            Route: "/timeline");

        await reporter.ReportExceptionAsync(new InvalidOperationException("Boom"), context);
        await reporter.ReportExceptionAsync(new InvalidOperationException("Boom"), context);

        Assert.Single(publisher.Submissions);
    }

    [Fact]
    public void AddStarWinInfrastructure_registers_the_exception_reporter()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddStarWinInfrastructure(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StarWin:DatabaseProvider"] = "Sqlite",
                ["ConnectionStrings:StarWin"] = "Data Source=:memory:"
            })
            .Build());

        using var serviceProvider = services.BuildServiceProvider();
        var reporter = serviceProvider.GetRequiredService<IStarWinExceptionReporter>();

        Assert.IsType<StarWinExceptionReporter>(reporter);
    }

    private sealed class FakeGitHubIssuePublisher : IGitHubIssuePublisher
    {
        public List<GitHubIssueSubmission> Submissions { get; } = [];

        public GitHubIssueSubmissionResult Publish(GitHubIssueSubmission submission)
        {
            Submissions.Add(submission);
            return new GitHubIssueSubmissionResult(true, true, "https://github.com/darkdhamon/Starforged-Atlas/issues/77");
        }
    }
}
