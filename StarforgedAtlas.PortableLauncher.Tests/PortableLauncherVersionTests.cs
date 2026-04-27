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
            PortablePackageInstaller.InstallPackage(
                stagingRoot,
                targetRoot,
                preservedDataRoot,
                Path.Combine(root, "backup"));

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
                Path.Combine(root, "backup"),
                new CollectingProgress<UpdateProgressInfo>(reportedProgress));

            Assert.Collection(
                reportedProgress,
                update => Assert.Equal("Creating a rollback backup...", update.Message),
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
            @"C:\temp\backup",
            "2026-04-27.0-developer-preview",
            "2026-04-26.2-developer-preview",
            4321);

        Assert.Equal(13, startInfo.ArgumentList.Count);
        Assert.Equal("--apply-update", startInfo.ArgumentList[0]);
        Assert.Equal("--staging-root", startInfo.ArgumentList[1]);
        Assert.Equal(@"C:\temp\package", startInfo.ArgumentList[2]);
        Assert.Equal("--target-root", startInfo.ArgumentList[3]);
        Assert.Equal(@"C:\portable\Starforged-Atlas-Portable\", startInfo.ArgumentList[4]);
        Assert.Equal("--backup-root", startInfo.ArgumentList[5]);
        Assert.Equal(@"C:\temp\backup", startInfo.ArgumentList[6]);
        Assert.Equal("--release-tag", startInfo.ArgumentList[7]);
        Assert.Equal("2026-04-27.0-developer-preview", startInfo.ArgumentList[8]);
        Assert.Equal("--previous-version", startInfo.ArgumentList[9]);
        Assert.Equal("2026-04-26.2-developer-preview", startInfo.ArgumentList[10]);
        Assert.Equal("--wait-for-pid", startInfo.ArgumentList[11]);
        Assert.Equal("4321", startInfo.ArgumentList[12]);
    }

    [Fact]
    public void CreateRestoreBackupHelperStartInfo_preserves_restore_arguments()
    {
        var startInfo = global::PortableLauncher.CreateRestoreBackupHelperStartInfo(
            @"C:\temp\StarforgedAtlas.UpdateHelper.exe",
            @"C:\temp",
            @"C:\temp\backup",
            @"C:\portable\Starforged-Atlas-Portable\",
            "2026-04-27.0-developer-preview",
            "2026-04-26.2-developer-preview",
            4321);

        Assert.Equal(11, startInfo.ArgumentList.Count);
        Assert.Equal("--restore-backup", startInfo.ArgumentList[0]);
        Assert.Equal("--backup-root", startInfo.ArgumentList[1]);
        Assert.Equal(@"C:\temp\backup", startInfo.ArgumentList[2]);
        Assert.Equal("--target-root", startInfo.ArgumentList[3]);
        Assert.Equal(@"C:\portable\Starforged-Atlas-Portable\", startInfo.ArgumentList[4]);
        Assert.Equal("--release-tag", startInfo.ArgumentList[5]);
        Assert.Equal("2026-04-27.0-developer-preview", startInfo.ArgumentList[6]);
        Assert.Equal("--previous-version", startInfo.ArgumentList[7]);
        Assert.Equal("2026-04-26.2-developer-preview", startInfo.ArgumentList[8]);
        Assert.Equal("--wait-for-pid", startInfo.ArgumentList[9]);
        Assert.Equal("4321", startInfo.ArgumentList[10]);
    }

    [Fact]
    public void RestoreBackup_restores_original_portable_files()
    {
        var root = Path.Combine(Path.GetTempPath(), $"portable-launcher-restore-test-{Guid.NewGuid():N}");
        var targetRoot = Path.Combine(root, "target");
        var stagingRoot = Path.Combine(root, "staging");
        var preservedDataRoot = Path.Combine(root, "preserved-data");
        var backupRoot = Path.Combine(root, "backup");

        Directory.CreateDirectory(Path.Combine(targetRoot, "app", "data"));
        Directory.CreateDirectory(Path.Combine(stagingRoot, "app"));

        File.WriteAllText(Path.Combine(targetRoot, "app", "old.txt"), "old");
        File.WriteAllText(Path.Combine(targetRoot, "app", "data", "settings.json"), "{ \"theme\": \"legacy\" }");
        File.WriteAllText(Path.Combine(stagingRoot, "app", "new.txt"), "new");
        File.WriteAllText(Path.Combine(stagingRoot, "Starforged Atlas.exe"), "launcher");

        try
        {
            PortablePackageInstaller.InstallPackage(stagingRoot, targetRoot, preservedDataRoot, backupRoot);

            File.WriteAllText(Path.Combine(targetRoot, "app", "new.txt"), "modified");
            File.WriteAllText(Path.Combine(targetRoot, "app", "data", "settings.json"), "{ \"theme\": \"updated\" }");

            PortablePackageInstaller.RestoreBackup(backupRoot, targetRoot);

            Assert.True(File.Exists(Path.Combine(targetRoot, "app", "old.txt")));
            Assert.False(File.Exists(Path.Combine(targetRoot, "app", "new.txt")));
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
    public void IgnoreRelease_persists_release_tag_for_future_checks()
    {
        var root = Path.Combine(Path.GetTempPath(), $"portable-launcher-state-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "app", "data"));

        try
        {
            PortableUpdateStateStore.IgnoreRelease(root, "2026-04-27.0-developer-preview");

            var state = PortableUpdateStateStore.Load(root);

            Assert.True(state.ShouldIgnore("2026-04-27.0-developer-preview"));
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
    public void PendingValidation_round_trips_through_portable_state_store()
    {
        var root = Path.Combine(Path.GetTempPath(), $"portable-launcher-validation-state-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "app", "data"));

        try
        {
            PortableUpdateStateStore.SavePendingValidation(
                root,
                new PortableUpdateValidationContext(
                    root,
                    "2026-04-27.0-developer-preview",
                    "2026-04-26.2-developer-preview",
                    @"C:\temp\backup"));

            var pending = PortableUpdateStateStore.LoadPendingValidation(root);

            Assert.NotNull(pending);
            Assert.Equal("2026-04-27.0-developer-preview", pending!.ReleaseTag);
            Assert.Equal("2026-04-26.2-developer-preview", pending.PreviousVersion);
            Assert.Equal(@"C:\temp\backup", pending.BackupRoot);

            PortableUpdateStateStore.ClearPendingValidation(root);

            Assert.Null(PortableUpdateStateStore.LoadPendingValidation(root));
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
    public void IgnoreRelease_preserves_pending_validation_state()
    {
        var root = Path.Combine(Path.GetTempPath(), $"portable-launcher-combined-state-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "app", "data"));

        try
        {
            PortableUpdateStateStore.SavePendingValidation(
                root,
                new PortableUpdateValidationContext(
                    root,
                    "2026-04-27.0-developer-preview",
                    "2026-04-26.2-developer-preview",
                    @"C:\temp\backup"));

            PortableUpdateStateStore.IgnoreRelease(root, "2026-04-27.0-developer-preview");

            var state = PortableUpdateStateStore.Load(root);

            Assert.True(state.ShouldIgnore("2026-04-27.0-developer-preview"));
            Assert.NotNull(state.PendingValidation);
            Assert.Equal("2026-04-27.0-developer-preview", state.PendingValidation!.ReleaseTag);
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
    public void BuildIssueDraftUri_contains_release_context()
    {
        var body = PortableUpdateFailureReporter.BuildIssueBody(
            "2026-04-27.0-developer-preview",
            "2026-04-26.2-developer-preview");
        var issueUri = PortableUpdateFailureReporter.BuildIssueDraftUri(
            "Bugfix: portable update rollback for 2026-04-27.0-developer-preview",
            body);

        Assert.Contains("issues/new", issueUri.ToString(), StringComparison.Ordinal);
        Assert.Contains("2026-04-27.0-developer-preview", Uri.UnescapeDataString(issueUri.Query), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAvailableUpdatePromptMessage_includes_release_notes()
    {
        var message = global::PortableLauncher.BuildAvailableUpdatePromptMessage(
            new PortableReleaseUpdate(
                "2026-04-26.2-developer-preview",
                "2026-04-27.0-developer-preview",
                "Developer Preview",
                "https://github.com/darkdhamon/Slip-Map-Tool/releases/download/2026-04-27.0-developer-preview/Starforged-Atlas-Portable.zip",
                "- Added desktop update notes"));

        Assert.Contains("Release notes:", message, StringComparison.Ordinal);
        Assert.Contains("- Added desktop update notes", message, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatReleaseNotesForPrompt_returns_fallback_when_notes_missing()
    {
        var notes = global::PortableLauncher.FormatReleaseNotesForPrompt(null);

        Assert.Equal("No release notes were provided for this release.", notes);
    }

    private sealed class CollectingProgress<T>(List<T> values) : IProgress<T>
    {
        public void Report(T value) => values.Add(value);
    }
}
