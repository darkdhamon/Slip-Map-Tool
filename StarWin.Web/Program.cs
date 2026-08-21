using StarWin.Application.Services;
using StarWin.Infrastructure.Services;
using StarWin.Web;
using System.Reflection;

Microsoft.AspNetCore.Builder.WebApplicationBuilder? builder = null;
IStarWinExceptionReporter startupExceptionReporter = new StarWinExceptionReporter();

try
{
    builder = StarWinWebHost.CreateBuilder(args);
    startupExceptionReporter = new StarWinExceptionReporter(
        new StarforgedAtlas.GitHubReporting.GitHubIssuePublisher(
            new StarforgedAtlas.GitHubReporting.ProcessGitHubCommandRunner()),
        builder.Configuration);
    var app = StarWinWebHost.Build(builder);

    await StarWinWebHost.InitializeAsync(app);
    await app.RunAsync();
}
catch (Exception ex) when (StarWinWebHost.ShouldReportStartupException(ex))
{
    await startupExceptionReporter.ReportExceptionAsync(
        ex,
        new StarWinExceptionContext(
            HostKind: "Web",
            Operation: "Standalone web host startup",
            AppVersion: ResolveAppVersion(builder?.Configuration)));
    throw;
}

static string ResolveAppVersion(Microsoft.Extensions.Configuration.IConfiguration? configuration)
{
    var configuredVersion = configuration?["StarforgedAtlas:AppVersion"];
    if (!string.IsNullOrWhiteSpace(configuredVersion))
    {
        return configuredVersion;
    }

    var assembly = typeof(StarWinWebHost).Assembly;
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "0.0.0";
}
