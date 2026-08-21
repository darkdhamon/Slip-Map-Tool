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
    GitHubCommandResult Run(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}

public interface IGitHubIssuePublisher
{
    GitHubIssueSubmissionResult Publish(
        GitHubIssueSubmission submission,
        CancellationToken cancellationToken = default);
}

public sealed class ProcessGitHubCommandRunner(TimeSpan? timeout = null) : IGitHubCommandRunner
{
    private readonly TimeSpan timeout = timeout ?? TimeSpan.FromSeconds(15);

    public GitHubCommandResult Run(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
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

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            process.WaitForExitAsync(timeoutSource.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            TryTerminate(process);
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException($"GitHub CLI did not finish within {timeout.TotalSeconds:0} seconds.");
        }

        var standardOutput = standardOutputTask.GetAwaiter().GetResult();
        var standardError = standardErrorTask.GetAwaiter().GetResult();

        return new GitHubCommandResult(process.ExitCode, standardOutput, standardError);
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
        }
        catch
        {
        }
    }
}

public sealed class GitHubIssuePublisher(IGitHubCommandRunner commandRunner) : IGitHubIssuePublisher
{
    public GitHubIssueSubmissionResult Publish(
        GitHubIssueSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

        if (!TryCreateIssue(submission, cancellationToken, out var issueUrl))
        {
            return new GitHubIssueSubmissionResult(
                IssueCreated: false,
                AddedToProject: false,
                IssueUrl: null);
        }

        var addedToProject = TryAddIssueToProject(submission.Target, issueUrl!, cancellationToken);
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

    private bool TryCreateIssue(
        GitHubIssueSubmission submission,
        CancellationToken cancellationToken,
        out string? issueUrl)
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

            var result = commandRunner.Run(arguments, cancellationToken);
            issueUrl = result.StandardOutput.Trim();
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(issueUrl);
        }
        catch
        {
            return false;
        }
    }

    private bool TryAddIssueToProject(
        GitHubIssueTarget target,
        string issueUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var projectNumber = TryResolveProjectNumber(
                target.ProjectOwner,
                target.ProjectTitle,
                cancellationToken);
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
            ], cancellationToken);

            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private int? TryResolveProjectNumber(
        string owner,
        string projectTitle,
        CancellationToken cancellationToken)
    {
        var result = commandRunner.Run(
        [
            "project",
            "list",
            "--owner",
            owner,
            "--format",
            "json"
        ], cancellationToken);

        if (result.ExitCode != 0)
        {
            return null;
        }

        var response = JsonSerializer.Deserialize<GitHubProjectListResponse>(
            result.StandardOutput,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        return response?.Projects.FirstOrDefault(project =>
            string.Equals(project.Title, projectTitle, StringComparison.OrdinalIgnoreCase))?.Number;
    }

    private sealed class GitHubProjectListResponse
    {
        public List<GitHubProjectSummary> Projects { get; set; } = [];
    }

    private sealed class GitHubProjectSummary
    {
        public string? Title { get; set; }

        public int Number { get; set; }
    }
}
