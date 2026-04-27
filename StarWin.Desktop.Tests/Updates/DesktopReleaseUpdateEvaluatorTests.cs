namespace StarWin.Desktop.Tests.Updates;

public sealed class DesktopReleaseUpdateEvaluatorTests
{
    [Fact]
    public void TryParse_accepts_developer_preview_tag_without_revision()
    {
        var parsed = StarforgedReleaseVersion.TryParse("2026-04-26-developer-preview", out var version);

        Assert.True(parsed);
        Assert.Equal(new StarforgedReleaseVersion(2026, 4, 26, 0), version);
    }

    [Fact]
    public void TryParse_accepts_revisioned_developer_preview_tag()
    {
        var parsed = StarforgedReleaseVersion.TryParse("2026-04-26.2-developer-preview", out var version);

        Assert.True(parsed);
        Assert.Equal(new StarforgedReleaseVersion(2026, 4, 26, 2), version);
    }

    [Fact]
    public void Evaluate_returns_prompt_when_latest_release_is_newer()
    {
        var prompt = DesktopReleaseUpdateEvaluator.Evaluate(
            new StarforgedReleaseVersion(2026, 4, 26, 0),
            "2026-04-26-developer-preview",
            skippedReleaseTag: null,
            new GitHubReleaseInfo(
                "2026-04-26.2-developer-preview",
                "https://github.com/darkdhamon/Slip-Map-Tool/releases/tag/2026-04-26.2-developer-preview",
                "Developer Preview"));

        Assert.NotNull(prompt);
        Assert.Equal("2026-04-26.2-developer-preview", prompt!.LatestReleaseTag);
    }

    [Fact]
    public void Evaluate_returns_null_when_latest_release_matches_skipped_tag()
    {
        var prompt = DesktopReleaseUpdateEvaluator.Evaluate(
            new StarforgedReleaseVersion(2026, 4, 26, 0),
            "2026-04-26-developer-preview",
            skippedReleaseTag: "2026-04-26.2-developer-preview",
            new GitHubReleaseInfo(
                "2026-04-26.2-developer-preview",
                "https://github.com/darkdhamon/Slip-Map-Tool/releases/tag/2026-04-26.2-developer-preview",
                "Developer Preview"));

        Assert.Null(prompt);
    }

    [Fact]
    public void Evaluate_returns_null_when_latest_release_is_not_newer()
    {
        var prompt = DesktopReleaseUpdateEvaluator.Evaluate(
            new StarforgedReleaseVersion(2026, 4, 26, 2),
            "2026-04-26.2-developer-preview",
            skippedReleaseTag: null,
            new GitHubReleaseInfo(
                "2026-04-26.2-developer-preview",
                "https://github.com/darkdhamon/Slip-Map-Tool/releases/tag/2026-04-26.2-developer-preview",
                "Developer Preview"));

        Assert.Null(prompt);
    }
}
