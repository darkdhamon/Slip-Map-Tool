using StarforgedAtlas.GitHubReporting;
using Xunit;

namespace StarWin.Infrastructure.Tests.GitHubReporting;

public sealed class GitHubIssuePublisherTests
{
    [Fact]
    public async Task Publish_creates_issue_and_adds_it_to_the_project_board()
    {
        var runner = new FakeGitHubCommandRunner(
        [
            new GitHubCommandResult(0, "https://github.com/darkdhamon/Starforged-Atlas/issues/9001", string.Empty),
            new GitHubCommandResult(0, """{"projects":[{"title":"Starforged Atlas Task Board","number":5}],"totalCount":1}""", string.Empty),
            new GitHubCommandResult(0, string.Empty, string.Empty)
        ]);
        var publisher = new GitHubIssuePublisher(runner);

        var result = await publisher.PublishAsync(new GitHubIssueSubmission(
            new GitHubIssueTarget(
                "darkdhamon/Starforged-Atlas",
                "darkdhamon",
                "Starforged Atlas Task Board"),
            "Bug: desktop InvalidOperationException during desktop startup",
            "body",
            ["bug"]));

        Assert.True(result.IssueCreated);
        Assert.True(result.AddedToProject);
        Assert.Equal("https://github.com/darkdhamon/Starforged-Atlas/issues/9001", result.IssueUrl);
        Assert.Equal(3, runner.Commands.Count);
        Assert.Contains("--repo", runner.Commands[0]);
        Assert.Contains("darkdhamon/Starforged-Atlas", runner.Commands[0]);
        Assert.Contains("item-add", runner.Commands[2]);
    }

    [Fact]
    public async Task Publish_returns_created_issue_when_project_add_fails()
    {
        var runner = new FakeGitHubCommandRunner(
        [
            new GitHubCommandResult(0, "https://github.com/darkdhamon/Starforged-Atlas/issues/9002", string.Empty),
            new GitHubCommandResult(1, string.Empty, "gh project list failed")
        ]);
        var publisher = new GitHubIssuePublisher(runner);

        var result = await publisher.PublishAsync(new GitHubIssueSubmission(
            new GitHubIssueTarget(
                "darkdhamon/Starforged-Atlas",
                "darkdhamon",
                "Starforged Atlas Task Board"),
            "Bug: web InvalidOperationException during request",
            "body",
            ["bug"]));

        Assert.True(result.IssueCreated);
        Assert.False(result.AddedToProject);
        Assert.Equal("https://github.com/darkdhamon/Starforged-Atlas/issues/9002", result.IssueUrl);
    }

    [Fact]
    public async Task Publish_retries_project_attachment_without_creating_a_second_issue()
    {
        var projectList = """{"projects":[{"title":"Starforged Atlas Task Board","number":5}],"totalCount":1}""";
        var runner = new FakeGitHubCommandRunner(
        [
            new GitHubCommandResult(0, "https://github.com/darkdhamon/Starforged-Atlas/issues/9003", string.Empty),
            new GitHubCommandResult(0, projectList, string.Empty),
            new GitHubCommandResult(1, string.Empty, "temporary item-add failure"),
            new GitHubCommandResult(0, projectList, string.Empty),
            new GitHubCommandResult(0, string.Empty, string.Empty)
        ]);
        var publisher = new GitHubIssuePublisher(runner);

        var result = await publisher.PublishAsync(new GitHubIssueSubmission(
            new GitHubIssueTarget(
                "darkdhamon/Starforged-Atlas",
                "darkdhamon",
                "Starforged Atlas Task Board"),
            "Bug: retry project attachment",
            "body",
            ["bug"]));

        Assert.True(result.IssueCreated);
        Assert.True(result.AddedToProject);
        Assert.Single(runner.Commands, command => command.Contains("create"));
        Assert.Equal(2, runner.Commands.Count(command => command.Contains("item-add")));
    }

    [Fact]
    public async Task PublishAsync_does_not_block_while_the_command_is_running()
    {
        var runner = new DeferredGitHubCommandRunner();
        var publisher = new GitHubIssuePublisher(runner);

        var publishTask = publisher.PublishAsync(CreateSubmission());

        Assert.False(publishTask.IsCompleted);
        runner.Complete(new GitHubCommandResult(1, string.Empty, "not available"));
        var result = await publishTask;
        Assert.False(result.IssueCreated);
    }

    [Fact]
    public async Task PublishAsync_propagates_command_cancellation()
    {
        var publisher = new GitHubIssuePublisher(new CancelingGitHubCommandRunner());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            publisher.PublishAsync(CreateSubmission()));
    }

    [Fact]
    public void BuildIssueDraftUri_includes_title_labels_and_body()
    {
        var uri = GitHubIssuePublisher.BuildIssueDraftUri(new GitHubIssueSubmission(
            new GitHubIssueTarget(
                "darkdhamon/Starforged-Atlas",
                "darkdhamon",
                "Starforged Atlas Task Board"),
            "Bug: desktop InvalidOperationException during startup",
            "Body text",
            ["bug"]));

        var decodedQuery = Uri.UnescapeDataString(uri.Query);
        Assert.Contains("issues/new", uri.ToString(), StringComparison.Ordinal);
        Assert.Contains("Bug: desktop InvalidOperationException during startup", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("labels=bug", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("Body text", decodedQuery, StringComparison.Ordinal);
    }

    private static GitHubIssueSubmission CreateSubmission()
    {
        return new GitHubIssueSubmission(
            new GitHubIssueTarget(
                "darkdhamon/Starforged-Atlas",
                "darkdhamon",
                "Starforged Atlas Task Board"),
            "Bug: test submission",
            "body",
            ["bug"]);
    }

    private sealed class FakeGitHubCommandRunner(IEnumerable<GitHubCommandResult> results) : IGitHubCommandRunner
    {
        private readonly Queue<GitHubCommandResult> results = new(results);

        public List<IReadOnlyList<string>> Commands { get; } = [];

        public Task<GitHubCommandResult> RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(arguments.ToArray());
            return Task.FromResult(this.results.Dequeue());
        }
    }

    private sealed class DeferredGitHubCommandRunner : IGitHubCommandRunner
    {
        private readonly TaskCompletionSource<GitHubCommandResult> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<GitHubCommandResult> RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
            => completion.Task.WaitAsync(cancellationToken);

        public void Complete(GitHubCommandResult result) => completion.SetResult(result);
    }

    private sealed class CancelingGitHubCommandRunner : IGitHubCommandRunner
    {
        public Task<GitHubCommandResult> RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
            => Task.FromException<GitHubCommandResult>(new OperationCanceledException());
    }
}
