using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StarWin.Application.Services;
using StarforgedAtlas.GitHubReporting;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinExceptionReporter : IStarWinExceptionReporter
{
    private static readonly GitHubIssueTarget DefaultIssueTarget = new(
        "darkdhamon/Starforged-Atlas",
        "darkdhamon",
        "Starforged Atlas Task Board");

    private readonly ConcurrentDictionary<string, DateTimeOffset> recentFingerprints = new(StringComparer.Ordinal);
    private readonly IGitHubIssuePublisher issuePublisher;
    private readonly IGitHubIssueDraftLauncher issueDraftLauncher;
    private readonly IConfiguration? configuration;
    private readonly ILogger<StarWinExceptionReporter> logger;
    private readonly TimeProvider timeProvider;

    public StarWinExceptionReporter()
        : this(
            new GitHubIssuePublisher(new ProcessGitHubCommandRunner()),
            issueDraftLauncher: new ProcessGitHubIssueDraftLauncher())
    {
    }

    public StarWinExceptionReporter(
        IGitHubIssuePublisher issuePublisher,
        IConfiguration? configuration = null,
        ILogger<StarWinExceptionReporter>? logger = null,
        TimeProvider? timeProvider = null,
        IGitHubIssueDraftLauncher? issueDraftLauncher = null)
    {
        this.issuePublisher = issuePublisher;
        this.issueDraftLauncher = issueDraftLauncher ?? new ProcessGitHubIssueDraftLauncher();
        this.configuration = configuration;
        this.logger = logger ?? NullLogger<StarWinExceptionReporter>.Instance;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task ReportExceptionAsync(
        Exception exception,
        StarWinExceptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(context);

        if (!ShouldReport(exception))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var issue = BuildIssue(exception, context, now);
        if (!TryReserveFingerprint(issue.Fingerprint, now))
        {
            logger.LogInformation(
                "Skipped duplicate exception report for {HostKind}. fingerprint={Fingerprint}",
                context.HostKind,
                issue.Fingerprint);
            return;
        }

        try
        {
            var submission = new GitHubIssueSubmission(
                ResolveIssueTarget(),
                issue.Title,
                issue.Body,
                ["bug"]);
            var result = await issuePublisher.PublishAsync(submission, cancellationToken);

            if (!result.IssueCreated)
            {
                recentFingerprints.TryRemove(issue.Fingerprint, out _);
                var draftOpened = issueDraftLauncher.TryOpen(submission);
                logger.LogWarning(
                    "Automatic GitHub exception reporting did not create an issue. hostKind={HostKind} fingerprint={Fingerprint} draftOpened={DraftOpened}",
                    context.HostKind,
                    issue.Fingerprint,
                    draftOpened);
            }
            else if (!result.AddedToProject)
            {
                logger.LogWarning(
                    "Automatic GitHub exception issue was created but not added to the task board. hostKind={HostKind} issueUrl={IssueUrl}",
                    context.HostKind,
                    result.IssueUrl);
            }
            else
            {
                logger.LogInformation(
                    "Automatic GitHub exception issue created successfully. hostKind={HostKind} issueUrl={IssueUrl}",
                    context.HostKind,
                    result.IssueUrl);
            }
        }
        catch (OperationCanceledException)
        {
            recentFingerprints.TryRemove(issue.Fingerprint, out _);
            throw;
        }
        catch (Exception ex)
        {
            recentFingerprints.TryRemove(issue.Fingerprint, out _);
            logger.LogWarning(
                ex,
                "Automatic GitHub exception reporting failed unexpectedly. hostKind={HostKind}",
                context.HostKind);
        }
    }

    internal static bool ShouldReport(Exception exception)
    {
        return exception is not OperationCanceledException;
    }

    internal static StarWinExceptionIssue BuildIssue(
        Exception exception,
        StarWinExceptionContext context,
        DateTimeOffset occurredAtUtc)
    {
        var location = SanitizeContextValue(
            context.Operation
                ?? (string.IsNullOrWhiteSpace(context.Route) ? "application" : context.Route));
        var title = TrimTitle($"Bug: {Normalize(context.HostKind)} {exception.GetType().Name} during {location}");

        var fingerprintSource = string.Join(
            "|",
            Normalize(context.HostKind),
            context.Operation ?? string.Empty,
            context.Route ?? string.Empty,
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message);
        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource)))[..16];

        var body = BuildIssueBody(exception, context, occurredAtUtc, fingerprint);
        return new StarWinExceptionIssue(title, body, fingerprint);
    }

    private static string BuildIssueBody(
        Exception exception,
        StarWinExceptionContext context,
        DateTimeOffset occurredAtUtc,
        string fingerprint)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Automated exception report");
        builder.AppendLine("This issue was created automatically after Starforged Atlas captured a qualifying exception.");
        builder.AppendLine();
        builder.AppendLine($"- Host kind: `{Normalize(context.HostKind)}`");
        builder.AppendLine($"- Occurred at (UTC): `{occurredAtUtc:O}`");
        builder.AppendLine($"- Exception type: `{exception.GetType().FullName ?? exception.GetType().Name}`");
        builder.AppendLine($"- Fingerprint: `{fingerprint}`");

        if (!string.IsNullOrWhiteSpace(context.Operation))
        {
            builder.AppendLine($"- Operation: `{SanitizeContextValue(context.Operation)}`");
        }

        if (!string.IsNullOrWhiteSpace(context.Route))
        {
            builder.AppendLine($"- Route/page: `{SanitizeContextValue(context.Route)}`");
        }

        if (!string.IsNullOrWhiteSpace(context.RequestId))
        {
            builder.AppendLine($"- Request ID: `{SanitizeContextValue(context.RequestId)}`");
        }

        if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
        {
            builder.AppendLine($"- Trace identifier: `{SanitizeContextValue(context.TraceIdentifier)}`");
        }

        if (!string.IsNullOrWhiteSpace(context.AppVersion))
        {
            builder.AppendLine($"- App version: `{SanitizeContextValue(context.AppVersion)}`");
        }

        if (context.AdditionalData is { Count: > 0 })
        {
            builder.AppendLine();
            builder.AppendLine("### Additional context");

            foreach (var pair in context.AdditionalData.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    builder.AppendLine($"- {SanitizeContextValue(pair.Key)}: `{SanitizeContextValue(pair.Value)}`");
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("### Exception");
        builder.AppendLine("```text");
        builder.AppendLine(BuildSafeExceptionDetails(exception));
        builder.AppendLine("```");

        return builder.ToString().TrimEnd();
    }

    private GitHubIssueTarget ResolveIssueTarget()
    {
        var repository = configuration?["StarforgedAtlas:IssueReporting:RepositoryFullName"];
        var owner = configuration?["StarforgedAtlas:IssueReporting:ProjectOwner"];
        var title = configuration?["StarforgedAtlas:IssueReporting:ProjectTitle"];

        return new GitHubIssueTarget(
            string.IsNullOrWhiteSpace(repository) ? DefaultIssueTarget.RepositoryFullName : repository,
            string.IsNullOrWhiteSpace(owner) ? DefaultIssueTarget.ProjectOwner : owner,
            string.IsNullOrWhiteSpace(title) ? DefaultIssueTarget.ProjectTitle : title);
    }

    private bool TryReserveFingerprint(string fingerprint, DateTimeOffset now)
    {
        const int duplicateWindowMinutes = 30;
        var duplicateWindow = TimeSpan.FromMinutes(duplicateWindowMinutes);

        while (true)
        {
            if (!recentFingerprints.TryGetValue(fingerprint, out var existing))
            {
                if (recentFingerprints.TryAdd(fingerprint, now))
                {
                    break;
                }

                continue;
            }

            if (now - existing < duplicateWindow)
            {
                return false;
            }

            if (recentFingerprints.TryUpdate(fingerprint, now, existing))
            {
                break;
            }
        }

        foreach (var pair in recentFingerprints.ToArray())
        {
            if (now - pair.Value >= duplicateWindow)
            {
                recentFingerprints.TryRemove(pair.Key, out _);
            }
        }

        return true;
    }

    private static string BuildSafeExceptionDetails(Exception exception)
    {
        var builder = new StringBuilder();
        var depth = 0;

        for (var current = exception; current is not null && depth < 8; current = current.InnerException)
        {
            if (depth > 0)
            {
                builder.AppendLine("Caused by:");
            }

            builder.AppendLine(current.GetType().FullName ?? current.GetType().Name);
            builder.AppendLine("Message: [redacted for privacy]");

            var frames = new StackTrace(current, fNeedFileInfo: false).GetFrames();
            if (frames is { Length: > 0 })
            {
                foreach (var frame in frames.Take(40))
                {
                    var method = frame.GetMethod();
                    if (method is null)
                    {
                        continue;
                    }

                    var typeName = method.DeclaringType?.FullName;
                    builder.Append("   at ");
                    if (!string.IsNullOrWhiteSpace(typeName))
                    {
                        builder.Append(typeName).Append('.');
                    }

                    builder.AppendLine(method.Name);
                }
            }

            depth++;
        }

        return builder.ToString().TrimEnd();
    }

    private static string SanitizeContextValue(string value)
    {
        const int maxLength = 500;
        var normalized = value.Replace('`', '\'').ReplaceLineEndings(" ").Trim();

        if (Path.IsPathFullyQualified(normalized)
            || normalized.Contains("password=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("pwd=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("token=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("secret=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("api_key=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("apikey=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("connectionstring=", StringComparison.OrdinalIgnoreCase))
        {
            return "[redacted]";
        }

        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength]}...";
    }

    private static string TrimTitle(string title)
    {
        const int maxLength = 220;
        return title.Length <= maxLength
            ? title
            : title[..maxLength].TrimEnd();
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Unknown"
            : value.Trim();
    }
}

internal sealed record StarWinExceptionIssue(string Title, string Body, string Fingerprint);
