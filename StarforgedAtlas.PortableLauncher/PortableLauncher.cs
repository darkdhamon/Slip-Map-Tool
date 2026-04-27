using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class PortableLauncher
{
    private const string DesktopExecutableName = "StarWin.Desktop.exe";
    private const string LauncherExecutableName = "Starforged Atlas.exe";
    private const string PortableZipAssetName = "Starforged-Atlas-Portable.zip";
    private const string SkipUpdateCheckArgument = "--skip-update-check";
    private const string ApplyUpdateArgument = "--apply-update";
    private const string StagingRootArgument = "--staging-root";
    private const string TargetRootArgument = "--target-root";
    private const string WaitForPidArgument = "--wait-for-pid";
    private const string HelperExecutableName = "StarforgedAtlas.UpdateHelper.exe";
    private static readonly Uri LatestReleaseApiUri = new("https://api.github.com/repos/darkdhamon/Slip-Map-Tool/releases/latest");

    public async Task RunAsync(string[] args)
    {
        if (args.Contains(ApplyUpdateArgument, StringComparer.OrdinalIgnoreCase))
        {
            await ApplyUpdateAsync(args);
            return;
        }

        var packageRoot = AppContext.BaseDirectory;
        var appRoot = Path.Combine(packageRoot, "app");
        var targetPath = Path.Combine(appRoot, DesktopExecutableName);

        if (!File.Exists(targetPath))
        {
            ShowError(
                "Starforged Atlas could not find the packaged desktop application.",
                $"Expected to find:{Environment.NewLine}{targetPath}");
            return;
        }

        try
        {
            var update = await TryGetAvailableUpdateAsync(targetPath);
            if (update is not null)
            {
                var releaseLabel = string.IsNullOrWhiteSpace(update.ReleaseName)
                    ? update.ReleaseTag
                    : $"{update.ReleaseName} ({update.ReleaseTag})";

                var prompt = $"A newer version of Starforged Atlas is available.{Environment.NewLine}{Environment.NewLine}Current version: {update.CurrentTag}{Environment.NewLine}Latest version: {releaseLabel}{Environment.NewLine}{Environment.NewLine}Download and install the update now?";
                var result = MessageBox.Show(
                    prompt,
                    "Starforged Atlas update available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    await DownloadAndApplyUpdateAsync(packageRoot, update);
                    return;
                }
            }

            StartDesktopApplication(targetPath, appRoot);
        }
        catch (Exception ex)
        {
            ShowError(
                "Starforged Atlas could not be started.",
                ex.GetBaseException().Message);
        }
    }

    private static void StartDesktopApplication(string targetPath, string appRoot)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = targetPath,
            WorkingDirectory = appRoot,
            UseShellExecute = true,
            Arguments = SkipUpdateCheckArgument
        };

        Process.Start(startInfo);
    }

    private async Task<PortableReleaseUpdate?> TryGetAvailableUpdateAsync(string desktopExecutablePath)
    {
        var currentTag = FileVersionInfo.GetVersionInfo(desktopExecutablePath).ProductVersion;
        if (!StarforgedReleaseVersion.TryParse(currentTag, out var currentVersion))
        {
            return null;
        }

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("StarforgedAtlasPortableLauncher", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await client.GetAsync(LatestReleaseApiUri);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var release = await JsonSerializer.DeserializeAsync<GitHubLatestReleaseResponse>(stream);
        if (release is null
            || string.IsNullOrWhiteSpace(release.TagName)
            || !StarforgedReleaseVersion.TryParse(release.TagName, out var latestVersion)
            || latestVersion.CompareTo(currentVersion) <= 0)
        {
            return null;
        }

        var asset = release.Assets.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, PortableZipAssetName, StringComparison.OrdinalIgnoreCase));
        if (asset is null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
        {
            return null;
        }

        return new PortableReleaseUpdate(
            currentTag ?? "0.0.0",
            release.TagName,
            release.Name,
            asset.BrowserDownloadUrl);
    }

    private async Task DownloadAndApplyUpdateAsync(string packageRoot, PortableReleaseUpdate update)
    {
        var stagingParent = Path.Combine(Path.GetTempPath(), "StarforgedAtlasUpdate", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        var zipPath = Path.Combine(stagingParent, PortableZipAssetName);
        var extractRoot = Path.Combine(stagingParent, "package");

        Directory.CreateDirectory(stagingParent);

        try
        {
            await LauncherProgressDialog.RunAsync(
                "Starforged Atlas update",
                "Downloading the latest portable release...",
                async progress =>
                {
                    using var client = new HttpClient
                    {
                        Timeout = TimeSpan.FromMinutes(5)
                    };

                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("StarforgedAtlasPortableLauncher", "1.0"));

                    progress.Report(new UpdateProgressInfo("Connecting to GitHub...", update.ReleaseTag, null));

                    using var response = await client.GetAsync(
                        update.DownloadUrl,
                        HttpCompletionOption.ResponseHeadersRead);

                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength;
                    await using var responseStream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = File.Create(zipPath);

                    await CopyDownloadToFileAsync(responseStream, fileStream, totalBytes, progress);

                    progress.Report(new UpdateProgressInfo("Extracting the portable package...", PortableZipAssetName, null));
                    ZipFile.ExtractToDirectory(zipPath, extractRoot);

                    var extractedDesktopPath = Path.Combine(extractRoot, "app", DesktopExecutableName);
                    if (!File.Exists(extractedDesktopPath))
                    {
                        throw new InvalidOperationException("The downloaded portable package is missing the desktop executable.");
                    }

                    progress.Report(new UpdateProgressInfo("Launching the installer...", "The updater will replace the portable files and restart the app.", 100));

                    var currentProcessId = Environment.ProcessId;
                    var helperExecutablePath = CreateUpdateHelperExecutable(stagingParent);
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = helperExecutablePath,
                        UseShellExecute = true,
                        WorkingDirectory = stagingParent,
                        Arguments =
                            $"{ApplyUpdateArgument} " +
                            $"{StagingRootArgument} \"{extractRoot}\" " +
                            $"{TargetRootArgument} \"{packageRoot}\" " +
                            $"{WaitForPidArgument} {currentProcessId}"
                    };

                    Process.Start(startInfo);
                });
        }
        catch
        {
            TryDeleteDirectory(stagingParent);
            throw;
        }
    }

    private static async Task CopyDownloadToFileAsync(
        Stream responseStream,
        Stream fileStream,
        long? totalBytes,
        IProgress<UpdateProgressInfo> progress)
    {
        var buffer = new byte[81920];
        long totalRead = 0;
        int read;

        while ((read = await responseStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;

            int? percent = totalBytes is > 0
                ? (int)Math.Clamp((double)totalRead / totalBytes.Value * 100d, 0d, 100d)
                : null;
            var detail = totalBytes is > 0
                ? $"{FormatBytes(totalRead)} of {FormatBytes(totalBytes.Value)}"
                : $"{FormatBytes(totalRead)} downloaded";

            progress.Report(new UpdateProgressInfo("Downloading update...", detail, percent));
        }
    }

    private static string FormatBytes(long byteCount)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        double size = byteCount;
        var suffixIndex = 0;

        while (size >= 1024d && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024d;
            suffixIndex++;
        }

        return suffixIndex == 0
            ? FormattableString.Invariant($"{size:0} {suffixes[suffixIndex]}")
            : FormattableString.Invariant($"{size:0.0} {suffixes[suffixIndex]}");
    }

    private static string CreateUpdateHelperExecutable(string stagingParent)
    {
        var currentExecutablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the current launcher executable path.");

        var helperExecutablePath = Path.Combine(stagingParent, HelperExecutableName);
        File.Copy(currentExecutablePath, helperExecutablePath, overwrite: true);
        return helperExecutablePath;
    }

    private async Task ApplyUpdateAsync(string[] args)
    {
        var stagingRoot = GetRequiredArgumentValue(args, StagingRootArgument);
        var targetRoot = GetRequiredArgumentValue(args, TargetRootArgument);
        var waitForPidValue = GetRequiredArgumentValue(args, WaitForPidArgument);

        if (!int.TryParse(waitForPidValue, NumberStyles.None, CultureInfo.InvariantCulture, out var waitForPid))
        {
            throw new InvalidOperationException("The update helper did not receive a valid process id to wait for.");
        }

        var preservedDataPath = Path.Combine(Path.GetTempPath(), "StarforgedAtlasUpdate", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), "data");
        Directory.CreateDirectory(Path.GetDirectoryName(preservedDataPath)!);

        try
        {
            await LauncherProgressDialog.RunAsync(
                "Installing Starforged Atlas update",
                "Preparing the portable update...",
                async progress =>
                {
                    progress.Report(new UpdateProgressInfo("Waiting for the launcher to close...", null, null));
                    await WaitForProcessExitAsync(waitForPid);

                    PortablePackageInstaller.InstallPackage(stagingRoot, targetRoot, preservedDataPath, progress);

                    progress.Report(new UpdateProgressInfo("Restarting Starforged Atlas...", null, 100));

                    var launcherPath = Path.Combine(targetRoot, LauncherExecutableName);
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = launcherPath,
                        WorkingDirectory = targetRoot,
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);
                });
        }
        catch (Exception ex)
        {
            ShowError(
                "Starforged Atlas could not apply the downloaded update.",
                ex.GetBaseException().Message);
        }
        finally
        {
            TryDeleteDirectory(Path.GetDirectoryName(preservedDataPath)!);
            TryDeleteDirectory(Path.GetDirectoryName(stagingRoot)!);
        }
    }

    private static async Task WaitForProcessExitAsync(int processId)
    {
        if (processId <= 0)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            await process.WaitForExitAsync();
        }
        catch
        {
        }
    }

    private static string GetRequiredArgumentValue(string[] args, string argumentName)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        throw new InvalidOperationException($"Missing required argument '{argumentName}'.");
    }

    internal static void CopyDirectory(string sourceRoot, string targetRoot, bool overwrite)
    {
        Directory.CreateDirectory(targetRoot);

        foreach (var directory in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, directory);
            Directory.CreateDirectory(Path.Combine(targetRoot, relativePath));
        }

        foreach (var file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, file);
            var destinationPath = Path.Combine(targetRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }

    private static void ShowError(string message, string detail)
    {
        var fullMessage = $"{message}{Environment.NewLine}{Environment.NewLine}{detail}";
        MessageBox.Show(
            fullMessage,
            "Starforged Atlas",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}

internal static class PortablePackageInstaller
{
    public static void InstallPackage(
        string stagingRoot,
        string targetRoot,
        string preservedDataPath,
        IProgress<UpdateProgressInfo>? progress = null)
    {
        progress?.Report(new UpdateProgressInfo("Preserving your portable data...", null, 15));
        PreservePortableData(targetRoot, preservedDataPath);
        progress?.Report(new UpdateProgressInfo("Replacing application files...", null, 55));
        ReplacePackageContents(stagingRoot, targetRoot);
        progress?.Report(new UpdateProgressInfo("Restoring your portable data...", null, 85));
        RestorePortableData(targetRoot, preservedDataPath);
        progress?.Report(new UpdateProgressInfo("Finishing the update...", null, 95));
    }

    private static void PreservePortableData(string targetRoot, string preservedDataPath)
    {
        var currentDataPath = Path.Combine(targetRoot, "app", "data");
        if (!Directory.Exists(currentDataPath))
        {
            return;
        }

        PortableLauncher.CopyDirectory(currentDataPath, preservedDataPath, overwrite: true);
    }

    private static void ReplacePackageContents(string stagingRoot, string targetRoot)
    {
        foreach (var directory in Directory.GetDirectories(targetRoot))
        {
            Directory.Delete(directory, recursive: true);
        }

        foreach (var file in Directory.GetFiles(targetRoot))
        {
            File.Delete(file);
        }

        PortableLauncher.CopyDirectory(stagingRoot, targetRoot, overwrite: true);
    }

    private static void RestorePortableData(string targetRoot, string preservedDataPath)
    {
        if (!Directory.Exists(preservedDataPath))
        {
            return;
        }

        var newDataPath = Path.Combine(targetRoot, "app", "data");
        Directory.CreateDirectory(newDataPath);
        PortableLauncher.CopyDirectory(preservedDataPath, newDataPath, overwrite: true);
    }
}

internal sealed record PortableReleaseUpdate(
    string CurrentTag,
    string ReleaseTag,
    string? ReleaseName,
    string DownloadUrl);

internal readonly record struct UpdateProgressInfo(string Message, string? Detail, int? Percent);

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

internal sealed class GitHubLatestReleaseResponse
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAsset> Assets { get; set; } = [];
}

internal sealed class GitHubReleaseAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}

internal static class LauncherProgressDialog
{
    public static Task RunAsync(
        string title,
        string initialMessage,
        Func<IProgress<UpdateProgressInfo>, Task> operation)
    {
        using var form = new UpdateProgressForm(title);
        Exception? failure = null;

        form.UpdateProgress(new UpdateProgressInfo(initialMessage, null, null));
        form.Shown += async (_, _) =>
        {
            var progress = new Progress<UpdateProgressInfo>(form.UpdateProgress);

            try
            {
                await operation(progress);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                if (!form.IsDisposed)
                {
                    form.BeginInvoke(form.Close);
                }
            }
        };

        form.ShowDialog();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return Task.CompletedTask;
    }
}
