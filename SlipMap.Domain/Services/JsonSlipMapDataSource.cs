using System.Text.Json;
using System.Text.Json.Serialization;
using SlipMap.Domain.Model.Entity;
using SlipMap.Domain.Model.Entity.Legacy;
using SlipMapEntity = SlipMap.Domain.Model.Entity.SlipMap;

namespace SlipMap.Domain.Services;

public sealed class JsonSlipMapDataSource : Abstract.ISlipMapDataSource
{
    private const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _saveDirectory;
    private readonly string _fileExtension;

    public JsonSlipMapDataSource(string? saveDirectory = null, string fileExtension = ".json")
    {
        _saveDirectory = string.IsNullOrWhiteSpace(saveDirectory) ? "SectorFiles" : saveDirectory;
        _fileExtension = fileExtension.StartsWith('.') ? fileExtension : $".{fileExtension}";
    }

    public async Task SaveAsync(string mapName, SlipMapEntity map, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapName);
        ArgumentNullException.ThrowIfNull(map);

        Directory.CreateDirectory(_saveDirectory);
        var filePath = GetSaveFilePath(mapName);
        await WriteAsync(filePath, map, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SlipMapEntity> LoadAsync(string mapName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapName);

        var filePath = GetSaveFilePath(mapName);
        return await ReadAsync(filePath, cancellationToken).ConfigureAwait(false);
    }

    public Task ExportAsync(string filePath, SlipMapEntity map, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(map);

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return WriteAsync(filePath, map, cancellationToken);
    }

    public Task<SlipMapEntity> ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return ReadAsync(filePath, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SlipMapEntity>> ImportAllAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<SlipMapExportJsonDocument>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The JSON slip map export file is empty or invalid.");

        if (document.SchemaVersion > CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Slip map JSON schema version {document.SchemaVersion} is newer than this app supports.");
        }

        var mapper = new LegacySlipMapMapper();
        return document.ToLegacySlipMapExport().Maps
            .Select(mapper.Map)
            .ToList();
    }

    public Task<IReadOnlyCollection<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_saveDirectory))
        {
            return Task.FromResult<IReadOnlyCollection<string>>([]);
        }

        var mapNames = Directory
            .EnumerateFiles(_saveDirectory, $"*{_fileExtension}")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        return Task.FromResult<IReadOnlyCollection<string>>(mapNames);
    }

    private async Task WriteAsync(string filePath, SlipMapEntity map, CancellationToken cancellationToken)
    {
        var document = ToDocument(map);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<SlipMapEntity> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<SlipMapJsonDocument>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The JSON slip map file is empty or invalid.");

        return FromDocument(document);
    }

    private string GetSaveFilePath(string mapName)
    {
        var fileName = Path.GetFileNameWithoutExtension(mapName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Map name must contain a valid file name.", nameof(mapName));
        }

        return Path.Combine(_saveDirectory, $"{fileName}{_fileExtension}");
    }

    private static SlipMapJsonDocument ToDocument(SlipMapEntity map)
    {
        return new SlipMapJsonDocument(
            CurrentSchemaVersion,
            SectorFileName: null,
            map.LastSystemId,
            map.CurrentSystemId,
            LegacySession: null,
            map.VisitedSystems
                .OrderBy(system => system.Id)
                .Select(system => new StarSystemJsonDocument(system.Id, system.Name, system.Notes))
                .ToList(),
            map.Routes
                .OrderBy(route => route.FirstSystemId)
                .ThenBy(route => route.SecondSystemId)
                .Select(route => new SlipRouteJsonDocument(route.FirstSystemId, route.SecondSystemId))
                .ToList());
    }

    private static SlipMapEntity FromDocument(SlipMapJsonDocument document)
    {
        if (document.SchemaVersion > CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Slip map JSON schema version {document.SchemaVersion} is newer than this app supports.");
        }

        return new LegacySlipMapMapper().Map(document.ToLegacySlipMap());
    }

    private sealed record SlipMapJsonDocument(
        int SchemaVersion,
        string? SectorFileName,
        int LastSystemId,
        int CurrentSystemId,
        LegacySessionJsonDocument? LegacySession,
        IReadOnlyList<StarSystemJsonDocument> StarSystems,
        IReadOnlyList<SlipRouteJsonDocument> Routes)
    {
        public LegacySlipMap ToLegacySlipMap()
        {
            return new LegacySlipMap
            {
                SchemaVersion = SchemaVersion,
                SectorFileName = SectorFileName,
                LastSystemId = LastSystemId,
                CurrentSystemId = CurrentSystemId,
                LegacySession = LegacySession?.ToLegacySession(),
                StarSystems = StarSystems
                    .Select(system => new LegacyStarSystem
                    {
                        Id = system.Id,
                        Name = system.Name,
                        Notes = system.Notes
                    })
                    .ToList(),
                Routes = Routes
                    .Select(route => new LegacySlipRoute
                    {
                        FirstSystemId = route.FirstSystemId,
                        SecondSystemId = route.SecondSystemId
                    })
                    .ToList()
            };
        }
    }

    private sealed record StarSystemJsonDocument(int Id, string? Name, string? Notes);

    private sealed record SlipRouteJsonDocument(int FirstSystemId, int SecondSystemId);

    private sealed record SlipMapExportJsonDocument(
        int SchemaVersion,
        DateTime ExportedAt,
        LegacySessionJsonDocument? LegacySession,
        IReadOnlyList<SlipMapJsonDocument> Maps)
    {
        public LegacySlipMapExport ToLegacySlipMapExport()
        {
            return new LegacySlipMapExport
            {
                SchemaVersion = SchemaVersion,
                ExportedAt = ExportedAt,
                LegacySession = LegacySession?.ToLegacySession(),
                Maps = Maps.Select(map => map.ToLegacySlipMap()).ToList()
            };
        }
    }

    private sealed record LegacySessionJsonDocument(
        string? DisplayName,
        int PilotSkill,
        string? SectorFileName,
        int? DestinationSystemId)
    {
        public LegacySession ToLegacySession()
        {
            return new LegacySession
            {
                DisplayName = DisplayName,
                PilotSkill = PilotSkill,
                SectorFileName = SectorFileName,
                DestinationSystemId = DestinationSystemId
            };
        }
    }
}
