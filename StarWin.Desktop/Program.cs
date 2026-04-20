using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Photino.NET;
using StarWin.Web;

const string WebView2Arguments = "--disable-gpu --disable-gpu-compositing --disable-features=CalculateNativeWinOcclusion --disable-backgrounding-occluded-windows";

var port = GetAvailablePort();
var localUrl = $"http://127.0.0.1:{port}";
var databasePath = StarWinDesktopPaths.GetDatabasePath();
var webViewDataPath = StarWinDesktopPaths.GetWebViewDataPath();
var smokeTest = args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase);

var builder = StarWinWebHost.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(StarWinWebHost).Assembly.GetName().Name,
    ContentRootPath = StarWinDesktopPaths.GetWebContentRoot()
});

builder.WebHost.UseUrls(localUrl);
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["StarWin:DatabaseProvider"] = "Sqlite",
    ["StarWin:ApplyMigrationsOnStartup"] = "true",
    ["StarforgedAtlas:HostKind"] = "Desktop",
    ["ConnectionStrings:StarWin"] = $"Data Source={databasePath}"
});

var app = StarWinWebHost.Build(builder);
await StarWinWebHost.InitializeAsync(app);
await app.StartAsync();

try
{
    if (smokeTest)
    {
        return;
    }

    var window = new PhotinoWindow()
        .SetTitle("Starforged Atlas")
        .SetUseOsDefaultSize(false)
        .SetSize(1280, 840);

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        Environment.SetEnvironmentVariable(
            "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
            WebView2Arguments);

        window.SetTemporaryFilesPath(webViewDataPath);
        window.SetBrowserControlInitParameters(WebView2Arguments);
    }

    var iconPath = StarWinDesktopPaths.GetIconPath();
    if (File.Exists(iconPath))
    {
        window.SetIconFile(iconPath);
    }

    window.Load(localUrl);

    window.WaitForClose();
}
finally
{
    await app.StopAsync();
    await app.DisposeAsync();
}

static int GetAvailablePort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    return ((IPEndPoint)listener.LocalEndpoint).Port;
}

internal static class StarWinDesktopPaths
{
    public static string GetDatabasePath()
    {
        return Path.Combine(GetApplicationDataRoot(), "starforged-atlas.db");
    }

    public static string GetWebViewDataPath()
    {
        var webViewRoot = Path.Combine(GetApplicationDataRoot(), "WebView2");
        Directory.CreateDirectory(webViewRoot);

        return webViewRoot;
    }

    public static string GetWebContentRoot()
    {
        return FindWebContentRoot(AppContext.BaseDirectory)
            ?? FindWebContentRoot(Environment.CurrentDirectory)
            ?? AppContext.BaseDirectory;
    }

    public static string GetIconPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Assets", "StarforgedAtlasLogo.ico");
    }

    private static string? FindWebContentRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "StarWin.Web");
            if (File.Exists(Path.Combine(candidate, "StarWin.Web.csproj")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string GetApplicationDataRoot()
    {
        var dataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var atlasRoot = Path.Combine(dataRoot, "Starforged Atlas");
        Directory.CreateDirectory(atlasRoot);

        return atlasRoot;
    }
}
