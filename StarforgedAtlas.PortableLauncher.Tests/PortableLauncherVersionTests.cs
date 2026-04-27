namespace StarforgedAtlas.PortableLauncher.Tests;

public sealed class PortableLauncherVersionTests
{
    [Fact]
    public void TryParse_accepts_revisioned_developer_preview_tag()
    {
        var parsed = StarforgedReleaseVersion.TryParse("2026-04-27.0-developer-preview", out var version);

        Assert.True(parsed);
        Assert.Equal(new StarforgedReleaseVersion(2026, 4, 27, 0), version);
    }

    [Fact]
    public void CompareTo_orders_newer_release_higher()
    {
        var older = new StarforgedReleaseVersion(2026, 4, 27, 0);
        var newer = new StarforgedReleaseVersion(2026, 4, 27, 1);

        Assert.True(newer.CompareTo(older) > 0);
    }

    [Fact]
    public void InstallPackage_replaces_app_files_and_preserves_existing_data()
    {
        var root = Path.Combine(Path.GetTempPath(), $"portable-launcher-test-{Guid.NewGuid():N}");
        var targetRoot = Path.Combine(root, "target");
        var stagingRoot = Path.Combine(root, "staging");
        var preservedDataRoot = Path.Combine(root, "preserved-data");

        Directory.CreateDirectory(Path.Combine(targetRoot, "app", "data"));
        Directory.CreateDirectory(Path.Combine(stagingRoot, "app"));

        File.WriteAllText(Path.Combine(targetRoot, "app", "old.txt"), "old");
        File.WriteAllText(Path.Combine(targetRoot, "app", "data", "settings.json"), "{ \"theme\": \"legacy\" }");
        File.WriteAllText(Path.Combine(stagingRoot, "app", "new.txt"), "new");
        File.WriteAllText(Path.Combine(stagingRoot, "Starforged Atlas.exe"), "launcher");

        try
        {
            PortablePackageInstaller.InstallPackage(stagingRoot, targetRoot, preservedDataRoot);

            Assert.False(File.Exists(Path.Combine(targetRoot, "app", "old.txt")));
            Assert.True(File.Exists(Path.Combine(targetRoot, "app", "new.txt")));
            Assert.Equal(
                "{ \"theme\": \"legacy\" }",
                File.ReadAllText(Path.Combine(targetRoot, "app", "data", "settings.json")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void InstallPackage_reports_progress_for_each_install_stage()
    {
        var root = Path.Combine(Path.GetTempPath(), $"portable-launcher-progress-test-{Guid.NewGuid():N}");
        var targetRoot = Path.Combine(root, "target");
        var stagingRoot = Path.Combine(root, "staging");
        var preservedDataRoot = Path.Combine(root, "preserved-data");
        var reportedProgress = new List<UpdateProgressInfo>();

        Directory.CreateDirectory(Path.Combine(targetRoot, "app", "data"));
        Directory.CreateDirectory(Path.Combine(stagingRoot, "app"));

        File.WriteAllText(Path.Combine(targetRoot, "app", "data", "settings.json"), "{ \"theme\": \"legacy\" }");
        File.WriteAllText(Path.Combine(stagingRoot, "app", "new.txt"), "new");
        File.WriteAllText(Path.Combine(stagingRoot, "Starforged Atlas.exe"), "launcher");

        try
        {
            PortablePackageInstaller.InstallPackage(
                stagingRoot,
                targetRoot,
                preservedDataRoot,
                new CollectingProgress<UpdateProgressInfo>(reportedProgress));

            Assert.Collection(
                reportedProgress,
                update => Assert.Equal("Preserving your portable data...", update.Message),
                update => Assert.Equal("Replacing application files...", update.Message),
                update => Assert.Equal("Restoring your portable data...", update.Message),
                update => Assert.Equal("Finishing the update...", update.Message));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateUpdateHelperStartInfo_preserves_trailing_backslash_arguments()
    {
        var startInfo = global::PortableLauncher.CreateUpdateHelperStartInfo(
            @"C:\temp\StarforgedAtlas.UpdateHelper.exe",
            @"C:\temp",
            @"C:\temp\package",
            @"C:\portable\Starforged-Atlas-Portable\",
            4321);

        Assert.Equal(7, startInfo.ArgumentList.Count);
        Assert.Equal("--apply-update", startInfo.ArgumentList[0]);
        Assert.Equal("--staging-root", startInfo.ArgumentList[1]);
        Assert.Equal(@"C:\temp\package", startInfo.ArgumentList[2]);
        Assert.Equal("--target-root", startInfo.ArgumentList[3]);
        Assert.Equal(@"C:\portable\Starforged-Atlas-Portable\", startInfo.ArgumentList[4]);
        Assert.Equal("--wait-for-pid", startInfo.ArgumentList[5]);
        Assert.Equal("4321", startInfo.ArgumentList[6]);
    }

    private sealed class CollectingProgress<T>(List<T> values) : IProgress<T>
    {
        public void Report(T value) => values.Add(value);
    }
}
