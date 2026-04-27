using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class DesktopReleaseUpdateService
{
    private static readonly Uri LatestReleaseApiUri = new("https://api.github.com/repos/darkdhamon/Slip-Map-Tool/releases/latest");
    private static readonly Uri LatestReleasePageUri = new("https://github.com/darkdhamon/Slip-Map-Tool/releases/latest");

    public async Task<DesktopReleaseUpdatePrompt?> CheckForUpdateAsync(CancellationToken cancellationToken)
    {
        var currentReleaseTag = DesktopAppVersion.GetCurrentReleaseTag();
        if (!StarforgedReleaseVersion.TryParse(currentReleaseTag, out var currentVersion))
        {
            return null;
        }

        var latestRelease = await TryGetLatestReleaseAsync(cancellationToken);
        if (latestRelease is null)
        {
            return null;
        }

        var skippedReleaseTag = DesktopUpdatePromptStateStore.Load().SkippedReleaseTag;
        return DesktopReleaseUpdateEvaluator.Evaluate(currentVersion, currentReleaseTag, skippedReleaseTag, latestRelease);
    }

    public void RememberSkippedRelease(string releaseTag)
    {
        DesktopUpdatePromptStateStore.Save(new DesktopUpdatePromptState
        {
            SkippedReleaseTag = releaseTag
        });
    }

    private static async Task<GitHubReleaseInfo?> TryGetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("StarforgedAtlasDesktop", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        try
        {
            using var response = await client.GetAsync(LatestReleaseApiUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<GitHubLatestReleaseResponse>(stream, cancellationToken: cancellationToken);
            if (payload is null || string.IsNullOrWhiteSpace(payload.TagName))
            {
                return null;
            }

            return new GitHubReleaseInfo(
                payload.TagName,
                string.IsNullOrWhiteSpace(payload.HtmlUrl) ? LatestReleasePageUri.AbsoluteUri : payload.HtmlUrl,
                payload.Name,
                payload.Body);
        }
        catch
        {
            return null;
        }
    }
}

internal static class DesktopAppVersion
{
    public static string GetCurrentReleaseTag()
    {
        var assembly = typeof(DesktopAppVersion).Assembly;
        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}

internal sealed record GitHubReleaseInfo(string TagName, string HtmlUrl, string? Name, string? Body);

internal sealed record DesktopReleaseUpdatePrompt(
    string CurrentReleaseTag,
    string LatestReleaseTag,
    string ReleaseUrl,
    string? ReleaseName,
    string? ReleaseNotes);

internal static class DesktopReleaseUpdateEvaluator
{
    public static DesktopReleaseUpdatePrompt? Evaluate(
        StarforgedReleaseVersion currentVersion,
        string currentReleaseTag,
        string? skippedReleaseTag,
        GitHubReleaseInfo latestRelease)
    {
        if (!StarforgedReleaseVersion.TryParse(latestRelease.TagName, out var latestVersion))
        {
            return null;
        }

        if (latestVersion.CompareTo(currentVersion) <= 0)
        {
            return null;
        }

        if (string.Equals(skippedReleaseTag, latestRelease.TagName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new DesktopReleaseUpdatePrompt(
            currentReleaseTag,
            latestRelease.TagName,
            latestRelease.HtmlUrl,
            latestRelease.Name,
            latestRelease.Body);
    }
}

internal static class DesktopReleaseUpdatePromptFormatter
{
    private const int MaxReleaseNotesLines = 12;
    private const int MaxReleaseNotesCharacters = 1200;

    public static string BuildMessage(DesktopReleaseUpdatePrompt prompt)
    {
        var releaseLabel = string.IsNullOrWhiteSpace(prompt.ReleaseName)
            ? prompt.LatestReleaseTag
            : $"{prompt.ReleaseName} ({prompt.LatestReleaseTag})";
        var releaseNotes = FormatReleaseNotes(prompt.ReleaseNotes);

        return
            $"A newer version of Starforged Atlas is available.{Environment.NewLine}{Environment.NewLine}" +
            $"Current version: {prompt.CurrentReleaseTag}{Environment.NewLine}" +
            $"Latest version: {releaseLabel}{Environment.NewLine}{Environment.NewLine}" +
            $"Release notes:{Environment.NewLine}{releaseNotes}{Environment.NewLine}{Environment.NewLine}" +
            "Open the latest GitHub release page so you can upgrade?";
    }

    public static string FormatReleaseNotes(string? releaseNotes)
    {
        if (string.IsNullOrWhiteSpace(releaseNotes))
        {
            return "No release notes were provided for this release.";
        }

        var normalized = releaseNotes.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
        var lines = normalized.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            lines[index] = lines[index].TrimEnd();
        }

        var includedLineCount = Math.Min(lines.Length, MaxReleaseNotesLines);
        var limited = string.Join(Environment.NewLine, lines, 0, includedLineCount);
        var truncated = lines.Length > MaxReleaseNotesLines;

        if (limited.Length > MaxReleaseNotesCharacters)
        {
            limited = limited[..MaxReleaseNotesCharacters].TrimEnd();
            truncated = true;
        }

        if (truncated)
        {
            limited = $"{limited}{Environment.NewLine}...{Environment.NewLine}See GitHub for the full release notes.";
        }

        return limited;
    }
}

internal readonly record struct StarforgedReleaseVersion(int Year, int Month, int Day, int Revision) : IComparable<StarforgedReleaseVersion>
{
    public int CompareTo(StarforgedReleaseVersion other)
    {
        var yearComparison = Year.CompareTo(other.Year);
        if (yearComparison != 0)
        {
            return yearComparison;
        }

        var monthComparison = Month.CompareTo(other.Month);
        if (monthComparison != 0)
        {
            return monthComparison;
        }

        var dayComparison = Day.CompareTo(other.Day);
        if (dayComparison != 0)
        {
            return dayComparison;
        }

        return Revision.CompareTo(other.Revision);
    }

    public static bool TryParse(string? value, out StarforgedReleaseVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();
        const string developerPreviewSuffix = "-developer-preview";
        if (trimmedValue.EndsWith(developerPreviewSuffix, StringComparison.OrdinalIgnoreCase))
        {
            trimmedValue = trimmedValue[..^developerPreviewSuffix.Length];
        }

        if (trimmedValue.StartsWith('v') || trimmedValue.StartsWith('V'))
        {
            trimmedValue = trimmedValue[1..];
        }

        var segments = trimmedValue.Split('.', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var datePortion = segments[0];
        var revisionPortion = segments.Length == 2 ? segments[1] : "0";

        var dateSegments = datePortion.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (dateSegments.Length != 3
            || !int.TryParse(dateSegments[0], out var year)
            || !int.TryParse(dateSegments[1], out var month)
            || !int.TryParse(dateSegments[2], out var day)
            || !int.TryParse(revisionPortion, out var revision))
        {
            return false;
        }

        try
        {
            _ = new DateOnly(year, month, day);
        }
        catch
        {
            return false;
        }

        version = new StarforgedReleaseVersion(year, month, day, revision);
        return true;
    }
}

internal sealed class DesktopUpdatePromptState
{
    public string? SkippedReleaseTag { get; set; }
}

internal static class DesktopUpdatePromptStateStore
{
    public static DesktopUpdatePromptState Load()
    {
        var path = StarWinDesktopPaths.GetUpdatePromptStatePath();
        if (!File.Exists(path))
        {
            return new DesktopUpdatePromptState();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DesktopUpdatePromptState>(json) ?? new DesktopUpdatePromptState();
        }
        catch
        {
            return new DesktopUpdatePromptState();
        }
    }

    public static void Save(DesktopUpdatePromptState state)
    {
        var path = StarWinDesktopPaths.GetUpdatePromptStatePath();
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }
}

internal sealed class GitHubLatestReleaseResponse
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
