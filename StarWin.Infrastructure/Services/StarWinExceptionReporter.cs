using System.Collections.Concurrent;
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
    private readonly IConfiguration? configuration;
    private readonly ILogger<StarWinExceptionReporter> logger;
    private readonly TimeProvider timeProvider;

    public StarWinExceptionReporter()
        : this(new GitHubIssuePublisher(new ProcessGitHubCommandRunner()))
    {
    }

    public StarWinExceptionReporter(
        IGitHubIssuePublisher issuePublisher,
        IConfiguration? configuration = null,
        ILogger<StarWinExceptionReporter>? logger = null,
        TimeProvider? timeProvider = null)
    {
        this.issuePublisher = issuePublisher;
        this.configuration = configuration;
        this.logger = logger ?? NullLogger<StarWinExceptionReporter>.Instance;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task ReportExceptionAsync(
        Exception exception,
        StarWinExceptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(context);

        if (!ShouldReport(exception))
        {
            return Task.CompletedTask;
        }

        var now = timeProvider.GetUtcNow();
        var issue = BuildIssue(exception, context, now);
        if (!TryReserveFingerprint(issue.Fingerprint, now))
        {
            logger.LogInformation(
                "Skipped duplicate exception report for {HostKind}. fingerprint={Fingerprint}",
                context.HostKind,
                issue.Fingerprint);
            return Task.CompletedTask;
        }

        try
        {
            var result = issuePublisher.Publish(new GitHubIssueSubmission(
                ResolveIssueTarget(),
                issue.Title,
                issue.Body,
                ["bug"]));

            if (!result.IssueCreated)
            {
                logger.LogWarning(
                    "Automatic GitHub exception reporting did not create an issue. hostKind={HostKind} fingerprint={Fingerprint}",
                    context.HostKind,
                    issue.Fingerprint);
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
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Automatic GitHub exception reporting failed unexpectedly. hostKind={HostKind}",
                context.HostKind);
        }

        return Task.CompletedTask;
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
        var location = context.Operation
            ?? (string.IsNullOrWhiteSpace(context.Route) ? "application" : context.Route);
        var title = TrimTitle($"Bug: {Normalize(context.HostKind)} {exception.GetType().Name} during {location}");

        var fingerprint = string.Join(
            "|",
            Normalize(context.HostKind),
            context.Operation ?? string.Empty,
            context.Route ?? string.Empty,
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message);

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
            builder.AppendLine($"- Operation: `{context.Operation}`");
        }

        if (!string.IsNullOrWhiteSpace(context.Route))
        {
            builder.AppendLine($"- Route/page: `{context.Route}`");
        }

        if (!string.IsNullOrWhiteSpace(context.RequestId))
        {
            builder.AppendLine($"- Request ID: `{context.RequestId}`");
        }

        if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
        {
            builder.AppendLine($"- Trace identifier: `{context.TraceIdentifier}`");
        }

        if (!string.IsNullOrWhiteSpace(context.AppVersion))
        {
            builder.AppendLine($"- App version: `{context.AppVersion}`");
        }

        if (context.AdditionalData is { Count: > 0 })
        {
            builder.AppendLine();
            builder.AppendLine("### Additional context");

            foreach (var pair in context.AdditionalData.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    builder.AppendLine($"- {pair.Key}: `{pair.Value}`");
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("### Exception");
        builder.AppendLine("```text");
        builder.AppendLine(exception.ToString());
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

        if (recentFingerprints.TryGetValue(fingerprint, out var existing)
            && now - existing < duplicateWindow)
        {
            return false;
        }

        recentFingerprints[fingerprint] = now;

        foreach (var pair in recentFingerprints.ToArray())
        {
            if (now - pair.Value >= duplicateWindow)
            {
                recentFingerprints.TryRemove(pair.Key, out _);
            }
        }

        return true;
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
