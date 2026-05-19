using System.Diagnostics;
using System.Text.Json;

namespace StarforgedAtlas.GitHubReporting;

public sealed record GitHubIssueTarget(
    string RepositoryFullName,
    string ProjectOwner,
    string ProjectTitle);

public sealed record GitHubIssueSubmission(
    GitHubIssueTarget Target,
    string Title,
    string Body,
    IReadOnlyList<string> Labels);

public sealed record GitHubIssueSubmissionResult(
    bool IssueCreated,
    bool AddedToProject,
    string? IssueUrl);

public sealed record GitHubCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);

public interface IGitHubCommandRunner
{
    GitHubCommandResult Run(IReadOnlyList<string> arguments);
}

public interface IGitHubIssuePublisher
{
    GitHubIssueSubmissionResult Publish(GitHubIssueSubmission submission);
}

public sealed class ProcessGitHubCommandRunner : IGitHubCommandRunner
{
    public GitHubCommandResult Run(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count == 0)
        {
            throw new ArgumentException("At least one gh argument is required.", nameof(arguments));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "gh",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the GitHub CLI.");

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new GitHubCommandResult(process.ExitCode, standardOutput, standardError);
    }
}

public sealed class GitHubIssuePublisher(IGitHubCommandRunner commandRunner) : IGitHubIssuePublisher
{
    public GitHubIssueSubmissionResult Publish(GitHubIssueSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        if (!TryCreateIssue(submission, out var issueUrl))
        {
            return new GitHubIssueSubmissionResult(
                IssueCreated: false,
                AddedToProject: false,
                IssueUrl: null);
        }

        var addedToProject = TryAddIssueToProject(submission.Target, issueUrl!);
        return new GitHubIssueSubmissionResult(
            IssueCreated: true,
            AddedToProject: addedToProject,
            IssueUrl: issueUrl);
    }

    public static Uri BuildIssueDraftUri(GitHubIssueSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var builder = new UriBuilder($"https://github.com/{submission.Target.RepositoryFullName}/issues/new");
        builder.Query =
            $"title={Uri.EscapeDataString(submission.Title)}&labels={Uri.EscapeDataString(string.Join(",", submission.Labels))}&body={Uri.EscapeDataString(submission.Body)}";
        return builder.Uri;
    }

    private bool TryCreateIssue(GitHubIssueSubmission submission, out string? issueUrl)
    {
        issueUrl = null;

        try
        {
            var arguments = new List<string>
            {
                "issue",
                "create",
                "--repo",
                submission.Target.RepositoryFullName
            };

            foreach (var label in submission.Labels.Where(label => !string.IsNullOrWhiteSpace(label)))
            {
                arguments.Add("--label");
                arguments.Add(label);
            }

            arguments.Add("--title");
            arguments.Add(submission.Title);
            arguments.Add("--body");
            arguments.Add(submission.Body);

            var result = commandRunner.Run(arguments);
            issueUrl = result.StandardOutput.Trim();
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(issueUrl);
        }
        catch
        {
            return false;
        }
    }

    private bool TryAddIssueToProject(GitHubIssueTarget target, string issueUrl)
    {
        try
        {
            var projectNumber = TryResolveProjectNumber(target.ProjectOwner, target.ProjectTitle);
            if (!projectNumber.HasValue)
            {
                return false;
            }

            var result = commandRunner.Run(
            [
                "project",
                "item-add",
                projectNumber.Value.ToString(),
                "--owner",
                target.ProjectOwner,
                "--url",
                issueUrl
            ]);

            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private int? TryResolveProjectNumber(string owner, string projectTitle)
    {
        var result = commandRunner.Run(
        [
            "project",
            "list",
            "--owner",
            owner,
            "--format",
            "json"
        ]);

        if (result.ExitCode != 0)
        {
            return null;
        }

        var projects = JsonSerializer.Deserialize<GitHubProjectSummary[]>(
            result.StandardOutput,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        return projects?.FirstOrDefault(project =>
            string.Equals(project.Title, projectTitle, StringComparison.OrdinalIgnoreCase))?.Number;
    }

    private sealed class GitHubProjectSummary
    {
        public string? Title { get; set; }

        public int Number { get; set; }
    }
}
