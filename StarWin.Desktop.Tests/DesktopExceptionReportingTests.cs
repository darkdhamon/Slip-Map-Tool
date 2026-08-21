using Microsoft.Extensions.Configuration;
using StarWin.Application.Services;
using StarforgedAtlas.GitHubReporting;

namespace StarWin.Desktop.Tests;

public sealed class DesktopExceptionReportingTests
{
    [Fact]
    public void Shell_startup_skips_duplicate_report_when_backend_already_reported()
    {
        Assert.False(DesktopExceptionObservation.ShouldReportShellStartupException(
            new DesktopBackendStartupReportedException()));
        Assert.True(DesktopExceptionObservation.ShouldReportShellStartupException(
            new TimeoutException("Unrelated timeout")));
    }

    [Fact]
    public async Task Background_task_failures_are_observed_and_reported()
    {
        var failure = new InvalidOperationException("Update failed");
        Exception? reported = null;

        await DesktopExceptionObservation.ObserveAsync(
            Task.FromException(failure),
            exception =>
            {
                reported = exception;
                return Task.CompletedTask;
            });

        Assert.Same(failure, reported);
    }

    [Fact]
    public async Task Desktop_reporter_uses_configured_issue_target()
    {
        var publisher = new FakeGitHubIssuePublisher();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StarforgedAtlas:IssueReporting:RepositoryFullName"] = "owner/configured-repo",
                ["StarforgedAtlas:IssueReporting:ProjectOwner"] = "configured-owner",
                ["StarforgedAtlas:IssueReporting:ProjectTitle"] = "Configured board"
            })
            .Build();
        var reporter = DesktopExceptionReporterFactory.Create(configuration, publisher);

        await reporter.ReportExceptionAsync(
            new InvalidOperationException("Boom"),
            new StarWinExceptionContext("Desktop", "Desktop shell startup"));

        var target = Assert.Single(publisher.Submissions).Target;
        Assert.Equal("owner/configured-repo", target.RepositoryFullName);
        Assert.Equal("configured-owner", target.ProjectOwner);
        Assert.Equal("Configured board", target.ProjectTitle);
    }

    [Fact]
    public void Backend_report_signal_is_suppressed_only_after_explicit_acknowledgment()
    {
        var nonce = Guid.NewGuid().ToString("N");

        Assert.False(DesktopBackendReportSignal.TryConsume(nonce));
        DesktopBackendReportSignal.MarkAttempted(nonce);
        Assert.True(DesktopBackendReportSignal.TryConsume(nonce));
        Assert.False(DesktopBackendReportSignal.TryConsume(nonce));
    }

    [Fact]
    public void Backend_child_arguments_identify_report_acknowledgment_nonce()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "StarWin.Desktop", "Program.cs"));

        Assert.Contains("args.Contains(BackendServerArgument", source, StringComparison.Ordinal);
        Assert.Contains("DesktopBackendReportSignal.MarkAttempted(reportNonce)", source, StringComparison.Ordinal);
    }

    private sealed class FakeGitHubIssuePublisher : IGitHubIssuePublisher
    {
        public List<GitHubIssueSubmission> Submissions { get; } = [];

        public Task<GitHubIssueSubmissionResult> PublishAsync(
            GitHubIssueSubmission submission,
            CancellationToken cancellationToken = default)
        {
            Submissions.Add(submission);
            return Task.FromResult(new GitHubIssueSubmissionResult(true, true, "https://example.test/issues/1"));
        }
    }
}
