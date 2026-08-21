using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;

namespace StarWin.Web.Components.Pages;

public partial class Error
{
    [Inject] private IStarWinExceptionReporter ExceptionReporter { get; set; } = default!;

    [CascadingParameter] private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override async Task OnInitializedAsync()
    {
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;

        var feature = HttpContext?.Features.Get<IExceptionHandlerPathFeature>();
        if (feature?.Error is null)
        {
            return;
        }

        var request = HttpContext?.Request;
        var additionalData = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Request method"] = request?.Method,
            ["Request path"] = feature.Path
        };

        var context = new StarWinExceptionContext(
                HostKind: ResolveHostKind(),
                Operation: "Unhandled web request",
                Route: feature.Path,
                RequestId: RequestId,
                TraceIdentifier: HttpContext?.TraceIdentifier,
                AppVersion: ResolveAppVersion(),
                AdditionalData: additionalData);
        _ = Task.Run(() => ExceptionReporter.ReportExceptionAsync(feature.Error, context));
    }

    private string ResolveHostKind()
    {
        var configuredHostKind = HttpContext?.RequestServices
            .GetService<IConfiguration>()?["StarforgedAtlas:HostKind"];
        return string.IsNullOrWhiteSpace(configuredHostKind)
            ? "Web"
            : configuredHostKind;
    }

    private string ResolveAppVersion()
    {
        var configuredVersion = HttpContext?.RequestServices
            .GetService<IConfiguration>()?["StarforgedAtlas:AppVersion"];
        if (!string.IsNullOrWhiteSpace(configuredVersion))
        {
            return configuredVersion;
        }

        var assembly = typeof(Error).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
