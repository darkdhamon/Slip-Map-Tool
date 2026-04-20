using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using StarWin.Web;

#if WINDOWS
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
#else
using Photino.NET;
#endif

internal static class Program
{
    private const string WebView2Arguments = "--disable-gpu --disable-gpu-compositing --disable-features=CalculateNativeWinOcclusion --disable-backgrounding-occluded-windows";

    [STAThread]
    public static async Task Main(string[] args)
    {
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

            RunDesktopShell(localUrl, webViewDataPath, StarWinDesktopPaths.GetIconPath());
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

#if WINDOWS
    private static void RunDesktopShell(string localUrl, string webViewDataPath, string iconPath)
    {
        Exception? shellException = null;
        var shellThread = new Thread(() =>
        {
            try
            {
                RunWindowsFormsShell(localUrl, webViewDataPath, iconPath);
            }
            catch (Exception ex)
            {
                shellException = ex;
            }
        });

        shellThread.SetApartmentState(ApartmentState.STA);
        shellThread.Start();
        shellThread.Join();

        if (shellException is not null)
        {
            throw shellException;
        }
    }

    private static void RunWindowsFormsShell(string localUrl, string webViewDataPath, string iconPath)
    {
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", WebView2Arguments);

        ApplicationConfiguration.Initialize();

        using var form = new Form
        {
            Text = "Starforged Atlas",
            Width = 1280,
            Height = 840,
            StartPosition = FormStartPosition.CenterScreen
        };

        if (File.Exists(iconPath))
        {
            form.Icon = new Icon(iconPath);
        }

        var webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.FromArgb(3, 7, 18)
        };

        form.Controls.Add(webView);
        form.Shown += async (_, _) =>
        {
            try
            {
                Console.WriteLine("Initializing Windows WebView2 desktop shell.");
                var options = new CoreWebView2EnvironmentOptions(WebView2Arguments);
                var environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: webViewDataPath,
                    options);

                await webView.EnsureCoreWebView2Async(environment);
                Console.WriteLine($"Windows WebView2 desktop shell loaded {localUrl}.");
                webView.CoreWebView2.Navigate(localUrl);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                MessageBox.Show(
                    form,
                    ex.Message,
                    "Starforged Atlas failed to start",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                form.Close();
            }
        };

        Application.Run(form);
    }
#else
    private static void RunDesktopShell(string localUrl, string webViewDataPath, string iconPath)
    {
        var window = new PhotinoWindow()
            .SetTitle("Starforged Atlas")
            .SetUseOsDefaultSize(false)
            .SetSize(1280, 840);

        if (File.Exists(iconPath))
        {
            window.SetIconFile(iconPath);
        }

        window.Load(localUrl);
        window.WaitForClose();
    }
#endif
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
