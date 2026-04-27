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
}
