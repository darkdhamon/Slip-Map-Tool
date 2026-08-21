using System.Diagnostics;

namespace StarforgedAtlas.GitHubReporting;

public interface IGitHubIssueDraftLauncher
{
    bool TryOpen(GitHubIssueSubmission submission);
}

public sealed class ProcessGitHubIssueDraftLauncher : IGitHubIssueDraftLauncher
{
    public bool TryOpen(GitHubIssueSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubIssuePublisher.BuildIssueDraftUri(submission).ToString(),
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
