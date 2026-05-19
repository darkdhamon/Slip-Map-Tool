using StarforgedAtlas.GitHubReporting;
using Xunit;

namespace StarWin.Infrastructure.Tests.GitHubReporting;

public sealed class GitHubIssuePublisherTests
{
    [Fact]
    public void Publish_creates_issue_and_adds_it_to_the_project_board()
    {
        var runner = new FakeGitHubCommandRunner(
        [
            new GitHubCommandResult(0, "https://github.com/darkdhamon/Starforged-Atlas/issues/9001", string.Empty),
            new GitHubCommandResult(0, """[{"title":"Starforged Atlas Task Board","number":5}]""", string.Empty),
            new GitHubCommandResult(0, string.Empty, string.Empty)
        ]);
        var publisher = new GitHubIssuePublisher(runner);

        var result = publisher.Publish(new GitHubIssueSubmission(
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
    public void Publish_returns_created_issue_when_project_add_fails()
    {
        var runner = new FakeGitHubCommandRunner(
        [
            new GitHubCommandResult(0, "https://github.com/darkdhamon/Starforged-Atlas/issues/9002", string.Empty),
            new GitHubCommandResult(1, string.Empty, "gh project list failed")
        ]);
        var publisher = new GitHubIssuePublisher(runner);

        var result = publisher.Publish(new GitHubIssueSubmission(
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

    private sealed class FakeGitHubCommandRunner(IEnumerable<GitHubCommandResult> results) : IGitHubCommandRunner
    {
        private readonly Queue<GitHubCommandResult> results = new(results);

        public List<IReadOnlyList<string>> Commands { get; } = [];

        public GitHubCommandResult Run(IReadOnlyList<string> arguments)
        {
            Commands.Add(arguments.ToArray());
            return this.results.Dequeue();
        }
    }
}
