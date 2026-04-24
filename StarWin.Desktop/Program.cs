using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StarWin.Web;

#if WINDOWS
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
#else
using Photino.NET;
#endif

internal static class Program
{
    private const string WebView2Arguments = "--disable-gpu --disable-gpu-compositing --disable-features=CalculateNativeWinOcclusion --disable-backgrounding-occluded-windows";
    private const string BackendServerArgument = "--backend-server";
    private const string BackendPortArgument = "--backend-port";
    private const string SmokeTestArgument = "--smoke-test";

    [STAThread]
    public static async Task Main(string[] args)
    {
        if (args.Contains(BackendServerArgument, StringComparer.OrdinalIgnoreCase))
        {
            var port = TryGetArgumentValue(args, BackendPortArgument, out var portValue) && int.TryParse(portValue, out var parsedPort)
                ? parsedPort
                : 5186;
            await RunBackendServerAsync(port, args);
            return;
        }

        var smokeTest = args.Contains(SmokeTestArgument, StringComparer.OrdinalIgnoreCase);
        using var startupReporter = CreateStartupReporter();

        try
        {
            startupReporter.Report("Preparing desktop shell", "Checking for a shared local backend.");
            await using var backendLease = await DesktopBackendCoordinator.AcquireAsync(startupReporter, CancellationToken.None);

            if (smokeTest)
            {
                startupReporter.Report("Desktop smoke test ready", $"Connected to shared backend at {backendLease.LocalUrl}.");
                return;
            }

            startupReporter.Report("Opening main window", "Starting the embedded browser shell.");
            RunDesktopShell(backendLease.LocalUrl, StarWinDesktopPaths.GetWebViewDataPath(), StarWinDesktopPaths.GetIconPath(), startupReporter);
        }
        catch (Exception ex)
        {
            startupReporter.Fail("Starforged Atlas failed to start", ex.GetBaseException().Message);
            throw;
        }
    }

    private static async Task RunBackendServerAsync(int port, string[] args)
    {
        var localUrl = $"http://127.0.0.1:{port}";
        var databasePath = StarWinDesktopPaths.GetDatabasePath();

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

        using var monitor = new DesktopBackendMonitor(app);

        await app.StartAsync();
        await monitor.RunAsync();

        try
        {
            await app.WaitForShutdownAsync();
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            DesktopBackendCoordinator.TryClearBackendRegistration(Environment.ProcessId);
        }
    }

    private static bool TryGetArgumentValue(string[] args, string argumentName, out string? value)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                value = args[index + 1];
                return true;
            }
        }

        value = null;
        return false;
    }

#if WINDOWS
    private static IDesktopStartupReporter CreateStartupReporter()
    {
        return DesktopStartupSplashScreen.Create();
    }
#else
    private static IDesktopStartupReporter CreateStartupReporter()
    {
        return new ConsoleStartupReporter();
    }
#endif

#if WINDOWS
    private static void RunDesktopShell(string localUrl, string webViewDataPath, string iconPath, IDesktopStartupReporter startupReporter)
    {
        Exception? shellException = null;
        var shellThread = new Thread(() =>
        {
            try
            {
                RunWindowsFormsShell(localUrl, webViewDataPath, iconPath, startupReporter);
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

    private static void RunWindowsFormsShell(string localUrl, string webViewDataPath, string iconPath, IDesktopStartupReporter startupReporter)
    {
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", WebView2Arguments);

        ApplicationConfiguration.Initialize();

        using var form = new Form
        {
            Text = "Starforged Atlas",
            Width = 1280,
            Height = 840,
            StartPosition = FormStartPosition.CenterScreen,
            WindowState = FormWindowState.Maximized
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

        var splashClosed = false;
        void CloseSplash()
        {
            if (splashClosed)
            {
                return;
            }

            splashClosed = true;
            startupReporter.Close();
        }

        form.Controls.Add(webView);
        form.Shown += async (_, _) =>
        {
            try
            {
                startupReporter.Report("Initializing browser engine", "Creating the local WebView environment.");
                var options = new CoreWebView2EnvironmentOptions(WebView2Arguments);
                var environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: webViewDataPath,
                    options);

                await webView.EnsureCoreWebView2Async(environment);
                startupReporter.Report("Loading Starforged Atlas", "Navigating to the shared local backend.");
                webView.CoreWebView2.NavigationCompleted += (_, navigationArgs) =>
                {
                    if (navigationArgs.IsSuccess)
                    {
                        CloseSplash();
                    }
                };
                webView.CoreWebView2.Navigate(localUrl);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                startupReporter.Fail("Starforged Atlas failed to start", ex.GetBaseException().Message);
                MessageBox.Show(
                    form,
                    ex.Message,
                    "Starforged Atlas failed to start",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                form.Close();
            }
        };

        form.FormClosed += (_, _) => CloseSplash();
        Application.Run(form);
    }
#else
    private static void RunDesktopShell(string localUrl, string webViewDataPath, string iconPath, IDesktopStartupReporter startupReporter)
    {
        var window = new PhotinoWindow()
            .SetTitle("Starforged Atlas")
            .SetUseOsDefaultSize(false)
            .SetSize(1280, 840);

        if (File.Exists(iconPath))
        {
            window.SetIconFile(iconPath);
        }

        startupReporter.Report("Loading Starforged Atlas", "Opening the shared local backend.");
        startupReporter.Close();
        window.Load(localUrl);
        window.WaitForClose();
    }
#endif
}

internal static class DesktopBackendCoordinator
{
    private const string StateMutexName = @"Local\StarforgedAtlas.Desktop.BackendState";
    private const string BackendServerArgument = "--backend-server";
    private const string BackendPortArgument = "--backend-port";

    public static async Task<DesktopBackendLease> AcquireAsync(
        IDesktopStartupReporter startupReporter,
        CancellationToken cancellationToken)
    {
        DesktopBackendState state;

        using (var mutex = CreateStateMutex())
        {
            WaitForMutex(mutex);
            try
            {
                state = LoadState();
                state.ClientProcessIds = state.ClientProcessIds
                    .Where(IsProcessAlive)
                    .Distinct()
                    .ToList();

                if (!state.ClientProcessIds.Contains(Environment.ProcessId))
                {
                    state.ClientProcessIds.Add(Environment.ProcessId);
                }

                if (!IsProcessAlive(state.BackendProcessId) || !IsPortResponsive(state.Port))
                {
                    startupReporter.Report("Starting shared backend", "Launching the local server used by all desktop windows.");
                    state.Port = SelectBackendPort(state.Port);
                    state.BackendProcessId = StartBackendProcess(state.Port).Id;
                }
                else
                {
                    startupReporter.Report("Connecting to shared backend", "Reusing the existing local server process.");
                }

                state.UpdatedAtUtc = DateTimeOffset.UtcNow;
                SaveState(state);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        await WaitForBackendReadyAsync(state.Port, startupReporter, cancellationToken);
        return new DesktopBackendLease($"http://127.0.0.1:{state.Port}");
    }

    public static void ReleaseClient(int processId)
    {
        using var mutex = CreateStateMutex();
        WaitForMutex(mutex);
        try
        {
            var state = LoadState();
            state.ClientProcessIds = state.ClientProcessIds
                .Where(pid => pid != processId && IsProcessAlive(pid))
                .Distinct()
                .ToList();
            state.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveState(state);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public static void TryClearBackendRegistration(int backendProcessId)
    {
        using var mutex = CreateStateMutex();
        WaitForMutex(mutex);
        try
        {
            var state = LoadState();
            if (state.BackendProcessId != backendProcessId)
            {
                return;
            }

            state.BackendProcessId = 0;
            state.Port = 0;
            state.ClientProcessIds = state.ClientProcessIds.Where(IsProcessAlive).Distinct().ToList();
            state.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveState(state);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public static DesktopBackendState LoadSharedState()
    {
        using var mutex = CreateStateMutex();
        WaitForMutex(mutex);
        try
        {
            var state = LoadState();
            var cleanedClientProcessIds = state.ClientProcessIds.Where(IsProcessAlive).Distinct().ToList();
            if (!state.ClientProcessIds.SequenceEqual(cleanedClientProcessIds))
            {
                state.ClientProcessIds = cleanedClientProcessIds;
                state.UpdatedAtUtc = DateTimeOffset.UtcNow;
                SaveState(state);
            }

            return state;
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static Mutex CreateStateMutex()
    {
        return new Mutex(false, StateMutexName);
    }

    private static void WaitForMutex(Mutex mutex)
    {
        if (!mutex.WaitOne(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("Timed out while waiting for the desktop backend state lock.");
        }
    }

    private static DesktopBackendState LoadState()
    {
        var statePath = StarWinDesktopPaths.GetBackendStatePath();
        if (!File.Exists(statePath))
        {
            return new DesktopBackendState();
        }

        try
        {
            var json = File.ReadAllText(statePath);
            return JsonSerializer.Deserialize<DesktopBackendState>(json) ?? new DesktopBackendState();
        }
        catch
        {
            return new DesktopBackendState();
        }
    }

    private static void SaveState(DesktopBackendState state)
    {
        var statePath = StarWinDesktopPaths.GetBackendStatePath();
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(statePath, json);
    }

    private static Process StartBackendProcess(int port)
    {
        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the current desktop executable path.");

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = $"{BackendServerArgument} {BackendPortArgument} {port}",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = AppContext.BaseDirectory
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to launch the shared desktop backend process.");
    }

    private static int SelectBackendPort(int preferredPort)
    {
        return preferredPort > 0 && IsPortAvailable(preferredPort)
            ? preferredPort
            : GetAvailablePort();
    }

    private static async Task WaitForBackendReadyAsync(
        int port,
        IDesktopStartupReporter startupReporter,
        CancellationToken cancellationToken)
    {
        if (port <= 0)
        {
            throw new InvalidOperationException("The shared desktop backend did not provide a valid local port.");
        }

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var healthUrl = $"http://127.0.0.1:{port}/desktop/health";
        var deadline = DateTime.UtcNow.AddMinutes(2);
        var attempt = 0;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;
            startupReporter.Report("Waiting for shared backend", $"The local server is starting up. Attempt {attempt:N0}.");

            try
            {
                using var response = await client.GetAsync(healthUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    startupReporter.Report("Shared backend ready", $"Connected to local server on port {port}.");
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException("The shared desktop backend did not become ready in time.");
    }

    private static bool IsPortResponsive(int port)
    {
        if (port <= 0)
        {
            return false;
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
            return connectTask.Wait(TimeSpan.FromMilliseconds(250)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static bool IsProcessAlive(int processId)
    {
        if (processId <= 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class DesktopBackendMonitor(WebApplication app) : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private Task? monitorTask;

    public Task RunAsync()
    {
        monitorTask = MonitorLoopAsync(cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var state = DesktopBackendCoordinator.LoadSharedState();
            if (state.BackendProcessId != 0 && state.BackendProcessId != Environment.ProcessId)
            {
                lifetime.StopApplication();
                return;
            }

            var remainingClientIds = state.ClientProcessIds
                .Where(pid => pid != Environment.ProcessId)
                .ToList();

            if (remainingClientIds.Count == 0)
            {
                lifetime.StopApplication();
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    public void Dispose()
    {
        cancellationTokenSource.Cancel();
        try
        {
            monitorTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
        }

        cancellationTokenSource.Dispose();
    }
}

internal sealed class DesktopBackendLease(string localUrl) : IAsyncDisposable, IDisposable
{
    private bool released;

    public string LocalUrl { get; } = localUrl;

    public void Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Release();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private void Release()
    {
        if (released)
        {
            return;
        }

        released = true;
        DesktopBackendCoordinator.ReleaseClient(Environment.ProcessId);
    }
}

internal sealed class DesktopBackendState
{
    public int Port { get; set; }

    public int BackendProcessId { get; set; }

    public List<int> ClientProcessIds { get; set; } = [];

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

internal interface IDesktopStartupReporter : IDisposable
{
    void Report(string title, string detail);

    void Fail(string title, string detail);

    void Close();
}

internal sealed class ConsoleStartupReporter : IDesktopStartupReporter
{
    public void Report(string title, string detail)
    {
        Console.WriteLine($"{title}: {detail}");
    }

    public void Fail(string title, string detail)
    {
        Console.Error.WriteLine($"{title}: {detail}");
    }

    public void Close()
    {
    }

    public void Dispose()
    {
    }
}

#if WINDOWS
internal sealed class DesktopStartupSplashScreen : IDesktopStartupReporter
{
    private readonly Thread uiThread;
    private readonly ManualResetEventSlim readyEvent = new(false);
    private readonly ManualResetEventSlim closedEvent = new(false);
    private SplashForm? form;

    private DesktopStartupSplashScreen()
    {
        uiThread = new Thread(RunUi)
        {
            IsBackground = true
        };
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();
        readyEvent.Wait();
    }

    public static DesktopStartupSplashScreen Create()
    {
        return new DesktopStartupSplashScreen();
    }

    public void Promote()
    {
        if (form?.IsHandleCreated != true)
        {
            return;
        }

        form.BeginInvoke(new Action(() => form.PromoteAboveShell()));
    }

    public void Report(string title, string detail)
    {
        if (form?.IsHandleCreated != true)
        {
            return;
        }

        form.BeginInvoke(new Action(() =>
        {
            form.UpdateStatus(title, detail, false);
            form.PromoteAboveShell();
        }));
    }

    public void Fail(string title, string detail)
    {
        if (form?.IsHandleCreated != true)
        {
            return;
        }

        form.BeginInvoke(new Action(() => form.UpdateStatus(title, detail, true)));
    }

    public void Close()
    {
        if (form?.IsHandleCreated == true)
        {
            form.BeginInvoke(new Action(() => form.Close()));
            closedEvent.Wait(TimeSpan.FromSeconds(5));
        }
    }

    public void Dispose()
    {
        Close();
        readyEvent.Dispose();
        closedEvent.Dispose();
    }

    private void RunUi()
    {
        ApplicationConfiguration.Initialize();
        using var splashForm = new SplashForm();
        form = splashForm;
        splashForm.Shown += (_, _) => readyEvent.Set();
        splashForm.FormClosed += (_, _) => closedEvent.Set();
        Application.Run(splashForm);
    }

    private sealed class SplashForm : Form
    {
        private readonly FrostedOverlayPanel contentPanel;
        private readonly OutlinedLabel titleLabel;
        private readonly OutlinedLabel detailLabel;
        private readonly ProgressBar progressBar;

        public SplashForm()
        {
            var splashImage = LoadSplashBackgroundImage();

            Text = "Starting Starforged Atlas";
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            Width = splashImage?.Width ?? 760;
            Height = splashImage?.Height ?? 760;
            BackColor = Color.Black;
            ForeColor = Color.FromArgb(226, 232, 240);
            TopMost = true;
            DoubleBuffered = true;
            BackgroundImage = splashImage;
            BackgroundImageLayout = ImageLayout.None;

            if (splashImage is not null)
            {
                Region = BuildRegionFromAlpha(splashImage);
            }

            contentPanel = new FrostedOverlayPanel
            {
                Width = 520,
                Height = 170,
                BackColor = Color.Transparent
            };

            titleLabel = new OutlinedLabel
            {
                AutoSize = false,
                Left = 34,
                Top = 28,
                Width = 452,
                Height = 50,
                Font = new Font("Segoe UI Semibold", 21f, FontStyle.Bold),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(248, 250, 252),
                ShadowColor = Color.FromArgb(200, 2, 6, 16),
                OutlineColor = Color.FromArgb(170, 1, 4, 14),
                Text = "Preparing desktop shell"
            };

            detailLabel = new OutlinedLabel
            {
                AutoSize = false,
                Left = 34,
                Top = 82,
                Width = 452,
                Height = 44,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Regular),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(235, 241, 248),
                ShadowColor = Color.FromArgb(200, 2, 6, 16),
                OutlineColor = Color.FromArgb(170, 1, 4, 14),
                Text = "Checking for a shared local backend."
            };

            progressBar = new ProgressBar
            {
                Left = 34,
                Top = 126,
                Width = 452,
                Height = 18,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 28
            };

            contentPanel.Controls.Add(progressBar);
            contentPanel.Controls.Add(detailLabel);
            contentPanel.Controls.Add(titleLabel);
            Controls.Add(contentPanel);

            Layout += (_, _) => CenterOverlay();
            Shown += (_, _) => PromoteAboveShell();
            CenterOverlay();
            PositionOnPrimaryScreen();
        }

        public void UpdateStatus(string title, string detail, bool isFailure)
        {
            titleLabel.Text = title;
            detailLabel.Text = detail;

            if (!isFailure)
            {
                return;
            }

            detailLabel.ForeColor = Color.FromArgb(248, 113, 113);
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.MarqueeAnimationSpeed = 0;
            progressBar.Value = 100;
        }

        private static Bitmap? LoadSplashBackgroundImage()
        {
            var splashImagePath = StarWinDesktopPaths.GetSplashImagePath();
            if (!File.Exists(splashImagePath))
            {
                return null;
            }

            try
            {
                return new Bitmap(splashImagePath);
            }
            catch
            {
                return null;
            }
        }

        private static Region BuildRegionFromAlpha(Bitmap bitmap)
        {
            var path = new GraphicsPath();
            const byte alphaThreshold = 10;

            for (var y = 0; y < bitmap.Height; y++)
            {
                var startX = -1;
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    var isOpaque = pixel.A > alphaThreshold;

                    if (isOpaque && startX < 0)
                    {
                        startX = x;
                    }
                    else if (!isOpaque && startX >= 0)
                    {
                        path.AddRectangle(new Rectangle(startX, y, x - startX, 1));
                        startX = -1;
                    }
                }

                if (startX >= 0)
                {
                    path.AddRectangle(new Rectangle(startX, y, bitmap.Width - startX, 1));
                }
            }

            return new Region(path);
        }

        public void PromoteAboveShell()
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            TopMost = true;
            NativeMethods.SetWindowPos(
                Handle,
                NativeMethods.HwndTopMost,
                0,
                0,
                0,
                0,
                NativeMethods.SetWindowPosFlags.NoMove
                | NativeMethods.SetWindowPosFlags.NoSize
                | NativeMethods.SetWindowPosFlags.ShowWindow);
            BringToFront();
        }

        private void CenterOverlay()
        {
            contentPanel.Left = (ClientSize.Width - contentPanel.Width) / 2;
            contentPanel.Top = (ClientSize.Height - contentPanel.Height) / 2 + 110;
        }

        private void PositionOnPrimaryScreen()
        {
            var primaryBounds = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromPoint(Point.Empty).WorkingArea;
            Left = primaryBounds.Left + ((primaryBounds.Width - Width) / 2);
            Top = primaryBounds.Top + ((primaryBounds.Height - Height) / 2);
        }
    }

    private sealed class FrostedOverlayPanel : Panel
    {
        public FrostedOverlayPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var shadowBounds = new Rectangle(10, 12, Width - 20, Height - 20);
            using var shadowPath = CreateRoundedRectangle(shadowBounds, 24);
            using var shadowBrush = new SolidBrush(Color.FromArgb(70, 3, 8, 20));
            e.Graphics.FillPath(shadowBrush, shadowPath);

            var glassBounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using var glassPath = CreateRoundedRectangle(glassBounds, 24);
            using var glassBrush = new SolidBrush(Color.FromArgb(132, 6, 14, 28));
            using var borderPen = new Pen(Color.FromArgb(120, 181, 212, 255), 1.2f);
            e.Graphics.FillPath(glassBrush, glassPath);
            e.Graphics.DrawPath(borderPen, glassPath);

            var glowBounds = new Rectangle(1, 1, Width - 3, Height / 2);
            using var glowPath = CreateRoundedRectangle(glowBounds, 22);
            using var glowBrush = new LinearGradientBrush(
                glowBounds,
                Color.FromArgb(56, 255, 255, 255),
                Color.FromArgb(12, 255, 255, 255),
                LinearGradientMode.Vertical);
            e.Graphics.FillPath(glowBrush, glowPath);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class OutlinedLabel : Control
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ShadowColor { get; set; } = Color.FromArgb(220, 0, 0, 0);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color OutlineColor { get; set; } = Color.FromArgb(180, 0, 0, 0);

        public OutlinedLabel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.UserPaint,
                true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var bounds = ClientRectangle;
            var flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis;

            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width, bounds.Height), ShadowColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X - 1, bounds.Y - 1, bounds.Width, bounds.Height), OutlineColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X, bounds.Y - 1, bounds.Width, bounds.Height), OutlineColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X + 1, bounds.Y - 1, bounds.Width, bounds.Height), OutlineColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X - 1, bounds.Y, bounds.Width, bounds.Height), OutlineColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X + 1, bounds.Y, bounds.Width, bounds.Height), OutlineColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X - 1, bounds.Y + 1, bounds.Width, bounds.Height), OutlineColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X, bounds.Y + 1, bounds.Width, bounds.Height), OutlineColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width, bounds.Height), OutlineColor, flags);
            TextRenderer.DrawText(e.Graphics, Text, Font, bounds, ForeColor, flags);
        }
    }

    private static class NativeMethods
    {
        public static readonly IntPtr HwndTopMost = new(-1);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            SetWindowPosFlags uFlags);

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            NoSize = 0x0001,
            NoMove = 0x0002,
            NoActivate = 0x0010,
            ShowWindow = 0x0040
        }
    }
}
#endif

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

    public static string GetSplashImagePath()
    {
        return Path.Combine(GetWebContentRoot(), "wwwroot", "assets", "images", "StarforgedAtlasLogo.png");
    }

    public static string GetBackendStatePath()
    {
        return Path.Combine(GetApplicationDataRoot(), "desktop-backend-state.json");
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
