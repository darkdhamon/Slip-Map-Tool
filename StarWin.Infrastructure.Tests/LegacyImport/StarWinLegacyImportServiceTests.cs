using System.IO.Compression;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StarWin.Domain.Model.Entity.Civilization;
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
    public async Task ImportStarWinZipAsync_bulk_inserts_alien_races_empires_and_contacts_into_sqlite()
    {
        var databasePath = CreateTempFilePath(".db");
        var zipPath = await BuildCivilizationLegacyZipAsync();

        try
        {
            await using var migrationContext = CreateDbContext(databasePath);
            await migrationContext.Database.EnsureCreatedAsync();

            var service = new StarWinLegacyImportService(CreateFactory(databasePath));

            await using var stream = File.OpenRead(zipPath);
            var result = await service.ImportStarWinZipAsync(stream, Path.GetFileName(zipPath), "Civilization Import Sector");

            Assert.True(result.Success);

            await using var verificationContext = CreateDbContext(databasePath);
            Assert.Equal(1, await verificationContext.AlienRaces.CountAsync());
            Assert.Equal(1, await verificationContext.Empires.CountAsync());
            Assert.Equal(1, await verificationContext.Set<EmpireRaceMembership>().CountAsync());
            Assert.Equal(1, await verificationContext.Set<EmpireContact>().CountAsync());

            var race = await verificationContext.AlienRaces.SingleAsync();
            var empire = await verificationContext.Empires
                .Include(item => item.Religions)
                .SingleAsync();
            var membership = await verificationContext.Set<EmpireRaceMembership>().SingleAsync();
            var contact = await verificationContext.Set<EmpireContact>().SingleAsync();

            Assert.Equal("Veloran", race.Name);
            Assert.Equal("Veloran State", empire.Name);
            Assert.Equal("Rare", race.HairType);
            Assert.Contains("Pair 1: Arms", race.LimbTypes);
            Assert.Contains("Pair 2: Legs", race.LimbTypes);
            Assert.Contains("Acute hearing", race.Abilities);
            Assert.Contains("Tail", race.BodyCharacteristics);
            Assert.Contains("Green", race.Colors);
            Assert.Contains("Blue", race.EyeColors);
            Assert.Contains("Black", race.HairColors);
            Assert.False(string.IsNullOrWhiteSpace(race.ImportDataJson));
            Assert.Equal("Anarchy", empire.GovernmentType);
            Assert.Single(empire.Religions);
            Assert.Equal("Veloran Tradition", empire.Religions[0].ReligionName);
            Assert.False(string.IsNullOrWhiteSpace(empire.ImportDataJson));
            Assert.Equal(1, await verificationContext.Religions.CountAsync());
            Assert.Equal(empire.Id, membership.EmpireId);
            Assert.Equal(race.Id, membership.RaceId);
            Assert.Equal(empire.Id, contact.EmpireId);
            Assert.Equal(77, contact.OtherEmpireId);
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

    [Fact]
    public void CreateDataTable_preserves_binary_column_types_for_bulk_copy()
    {
        var createDataTable = typeof(StarWinLegacyImportService)
            .GetMethod("CreateDataTable", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(createDataTable);

        var rows = new List<object?[]>
        {
            new object?[] { 1, new byte[] { 1, 2, 3 } }
        };

        var table = Assert.IsType<System.Data.DataTable>(
            createDataTable!.Invoke(null, new object?[] { new[] { "Id", "LegacyAttributes" }, rows }));

        Assert.Equal(typeof(int), table.Columns["Id"]!.DataType);
        Assert.Equal(typeof(byte[]), table.Columns["LegacyAttributes"]!.DataType);
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<byte[]>(table.Rows[0]["LegacyAttributes"]));
    }

    [Fact]
    public async Task ImportStarWinZipAsync_imports_delcora_fixture_into_sqlserver()
    {
        var databaseName = $"StarWinImportTest_{Guid.NewGuid():N}";
        var zipPath = GetDelcoraZipPath();

        try
        {
            await using var migrationContext = CreateSqlServerDbContext(databaseName);
            await migrationContext.Database.EnsureDeletedAsync();
            await migrationContext.Database.MigrateAsync();

            var service = new StarWinLegacyImportService(CreateSqlServerFactory(databaseName));

            await using var stream = File.OpenRead(zipPath);
            var result = await service.ImportStarWinZipAsync(stream, Path.GetFileName(zipPath), "Delcora SQL Server Test");

            Assert.True(result.Success);

            await using var verificationContext = CreateSqlServerDbContext(databaseName);
            Assert.Equal("Delcora SQL Server Test", await verificationContext.Sectors.Select(sector => sector.Name).SingleAsync());
            Assert.True(await verificationContext.StarSystems.AnyAsync());
            Assert.True(await verificationContext.Worlds.AnyAsync());
            Assert.True(await verificationContext.AlienRaces.AnyAsync());
            Assert.True(await verificationContext.Empires.AnyAsync());
            Assert.True(await verificationContext.Set<EmpireRaceMembership>().AnyAsync());
            Assert.True(await verificationContext.Set<EmpireContact>().AnyAsync());
        }
        finally
        {
            await using var cleanupContext = CreateSqlServerDbContext(databaseName);
            await cleanupContext.Database.EnsureDeletedAsync();
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

    private static IDbContextFactory<StarWinDbContext> CreateSqlServerFactory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<StarWinDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSqlServer(BuildSqlServerConnectionString(databaseName))
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

    private static StarWinDbContext CreateSqlServerDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<StarWinDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSqlServer(BuildSqlServerConnectionString(databaseName))
            .Options;

        return new StarWinDbContext(options);
    }

    private static async Task<string> BuildSmallLegacyZipAsync()
    {
        var zipPath = CreateTempFilePath(".zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        CopyBaselineStarMapEntries(archive);
        await WriteZipEntryFromBytesAsync(archive, "Test.aln", []);
        await WriteZipEntryFromBytesAsync(archive, "Test.col", []);
        await WriteZipEntryFromBytesAsync(archive, "Test.con", []);
        await WriteZipEntryFromBytesAsync(archive, "Test.emp", []);
        await WriteZipEntryAsync(archive, "Test.his", string.Empty);
        await WriteZipEntryAsync(archive, "Test.nam", string.Empty);
        return zipPath;
    }

    private static async Task<string> BuildCivilizationLegacyZipAsync()
    {
        var zipPath = CreateTempFilePath(".zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        CopyBaselineStarMapEntries(archive);
        await WriteZipEntryFromBytesAsync(archive, "Test.aln", CreateAlienRecordBytes());
        await WriteZipEntryFromBytesAsync(archive, "Test.col", []);
        await WriteZipEntryFromBytesAsync(archive, "Test.con", CreateContactRecordBytes());
        await WriteZipEntryFromBytesAsync(archive, "Test.emp", CreateEmpireRecordBytes());
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

    private static async Task WriteZipEntryFromBytesAsync(ZipArchive archive, string entryName, byte[] contents)
    {
        var entry = archive.CreateEntry(entryName);
        await using var stream = entry.Open();
        await stream.WriteAsync(contents);
    }

    private static void CopyBaselineStarMapEntries(ZipArchive archive)
    {
        var sourceDirectory = Path.Combine(
            GetRepositoryRoot(),
            "Legacy",
            "starwin v2 compiled app",
            "data",
            "Test");

        foreach (var extension in new[] { ".sun", ".pln", ".mon" })
        {
            var sourcePath = Path.Combine(sourceDirectory, $"Test{extension}");
            archive.CreateEntryFromFile(sourcePath, $"Test{extension}");
        }
    }

    private static byte[] CreateAlienRecordBytes()
    {
        var buffer = new byte[92];
        WriteInt32(buffer, 0, 0);
        buffer[4] = 1;
        buffer[5] = 1;
        buffer[6] = 2;
        buffer[7] = 1;
        buffer[8] = 1;
        buffer[9] = 1;
        buffer[10] = 1;
        buffer[11] = 1;
        buffer[12] = 1;
        buffer[18] = 5;
        buffer[19] = 3;
        WriteInt16(buffer, 14, 180);
        WriteInt16(buffer, 16, 220);
        for (var index = 0; index < 15; index++)
        {
            buffer[30 + index] = (byte)(index + 1);
        }

        buffer[45] = 1;
        buffer[57] = 64;
        buffer[59] = 2;
        buffer[61] = 1;
        buffer[63] = 32;
        buffer[65] = 2;
        buffer[67] = 1;
        buffer[68] = 1;
        buffer[69] = 7;
        WriteDelphiShortString(buffer, 70, 20, "Veloran");
        return buffer;
    }

    private static byte[] CreateEmpireRecordBytes()
    {
        var buffer = new byte[52];
        var values = new[] { 120, 85, 430, 15, 12, 34, 56, 7, 2, 1, 3, 4, 5 };
        for (var index = 0; index < values.Length; index++)
        {
            WriteInt32(buffer, index * 4, values[index]);
        }

        return buffer;
    }

    private static byte[] CreateContactRecordBytes()
    {
        var buffer = new byte[6];
        WriteUInt16(buffer, 0, 0);
        WriteUInt16(buffer, 2, 77);
        buffer[4] = 1;
        buffer[5] = 9;
        return buffer;
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        BitConverter.GetBytes(value).CopyTo(buffer, offset);
    }

    private static void WriteInt16(byte[] buffer, int offset, short value)
    {
        BitConverter.GetBytes(value).CopyTo(buffer, offset);
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        BitConverter.GetBytes(value).CopyTo(buffer, offset);
    }

    private static void WriteDelphiShortString(byte[] buffer, int offset, int maxLength, string value)
    {
        var bytes = System.Text.Encoding.Latin1.GetBytes(value);
        var length = Math.Min(maxLength, bytes.Length);
        buffer[offset] = (byte)length;
        Array.Copy(bytes, 0, buffer, offset + 1, length);
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

    private static string GetDelcoraZipPath()
    {
        return Path.Combine(
            GetRepositoryRoot(),
            "Legacy",
            "starwin v2 compiled app",
            "data",
            "Delcora Sector.zip");
    }

    private static string BuildSqlServerConnectionString(string databaseName)
    {
        return $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
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
