namespace StarWin.Application.Services.LegacyImport;

public sealed record StarWinLegacyStarSystemPreview(
    int LegacyId,
    string Name,
    short X,
    short Y,
    short Z,
    byte StarCount,
    IReadOnlyList<byte> PlanetCounts,
    IReadOnlyList<string> SpectralClasses,
    ushort AllegianceId);
