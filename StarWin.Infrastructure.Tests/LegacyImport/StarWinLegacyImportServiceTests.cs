using System.IO.Compression;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StarWin.Infrastructure.Data;
using StarWin.Infrastructure.Services;
using Xunit;

namespace StarWin.Infrastructure.Tests.LegacyImport;

public sealed class StarWinLegacyImportServiceTests
{
    [Fact]
    public async Task ImportStarWinZipAsync_imports_small_legacy_fixture_into_sqlite()
    {
        var databasePath = CreateTempFilePath(".db");
        var zipPath = await BuildSmallLegacyZipAsync();

        try
        {
            await using var migrationContext = CreateDbContext(databasePath);
            await migrationContext.Database.EnsureCreatedAsync();

            var service = new StarWinLegacyImportService(CreateFactory(databasePath));

            await using var stream = File.OpenRead(zipPath);
            var result = await service.ImportStarWinZipAsync(stream, Path.GetFileName(zipPath), "Regression Test Sector");

            Assert.True(result.Success);
            Assert.Equal("Test", result.Preview.Sectors.Single().SectorName);

            await using var verificationContext = CreateDbContext(databasePath);
            Assert.Equal("Regression Test Sector", await verificationContext.Sectors.Select(sector => sector.Name).SingleAsync());
            Assert.True(await verificationContext.StarSystems.AnyAsync());
            Assert.True(await verificationContext.Worlds.AnyAsync());
            Assert.True(await verificationContext.HistoryEvents.AnyAsync(history => history.EventType == "Legacy Import"));
        }
        finally
        {
            DeleteIfExists(zipPath);
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task ImportStarWinZipAsync_creates_a_fresh_db_context_for_each_import_call()
    {
        var databasePath = CreateTempFilePath(".db");
        var zipPath = await BuildSmallLegacyZipAsync();

        try
        {
            await using var migrationContext = CreateDbContext(databasePath);
            await migrationContext.Database.EnsureCreatedAsync();

            var countingFactory = new CountingDbContextFactory(CreateFactory(databasePath));
            var service = new StarWinLegacyImportService(countingFactory);

            await using (var firstStream = File.OpenRead(zipPath))
            {
                var firstResult = await service.ImportStarWinZipAsync(firstStream, Path.GetFileName(zipPath), "First Import Sector");
                Assert.True(firstResult.Success);
            }

            await using (var secondStream = File.OpenRead(zipPath))
            {
                var secondResult = await service.ImportStarWinZipAsync(secondStream, Path.GetFileName(zipPath), "Second Import Sector");
                Assert.True(secondResult.Success);
            }

            Assert.Equal(2, countingFactory.CreateCount);
        }
        finally
        {
            DeleteIfExists(zipPath);
            DeleteIfExists(databasePath);
        }
    }

    private static IDbContextFactory<StarWinDbContext> CreateFactory(string databasePath)
    {
        var options = new DbContextOptionsBuilder<StarWinDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new OptionsDbContextFactory(options);
    }

    private static StarWinDbContext CreateDbContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<StarWinDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new StarWinDbContext(options);
    }

    private static async Task<string> BuildSmallLegacyZipAsync()
    {
        var zipPath = CreateTempFilePath(".zip");
        var sourceDirectory = Path.Combine(
            GetRepositoryRoot(),
            "Legacy",
            "starwin v2 compiled app",
            "data",
            "Test");

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var extension in new[] { ".sun", ".pln", ".mon", ".aln", ".col", ".con", ".emp" })
        {
            var sourcePath = Path.Combine(sourceDirectory, $"Test{extension}");
            archive.CreateEntryFromFile(sourcePath, $"Test{extension}");
        }

        await WriteZipEntryAsync(archive, "Test.his", string.Empty);
        await WriteZipEntryAsync(archive, "Test.nam", string.Empty);
        return zipPath;
    }

    private static async Task WriteZipEntryAsync(ZipArchive archive, string entryName, string contents)
    {
        var entry = archive.CreateEntry(entryName);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(contents);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }

    private static string CreateTempFilePath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), $"starwin-import-{Guid.NewGuid():N}{extension}");
    }

    private static void DeleteIfExists(string path)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class CountingDbContextFactory(IDbContextFactory<StarWinDbContext> innerFactory) : IDbContextFactory<StarWinDbContext>
    {
        public int CreateCount { get; private set; }

        public StarWinDbContext CreateDbContext()
        {
            CreateCount++;
            return innerFactory.CreateDbContext();
        }

        public async Task<StarWinDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            CreateCount++;
            return await innerFactory.CreateDbContextAsync(cancellationToken);
        }
    }

    private sealed class OptionsDbContextFactory(DbContextOptions<StarWinDbContext> options) : IDbContextFactory<StarWinDbContext>
    {
        public StarWinDbContext CreateDbContext()
        {
            return new StarWinDbContext(options);
        }

        public Task<StarWinDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
