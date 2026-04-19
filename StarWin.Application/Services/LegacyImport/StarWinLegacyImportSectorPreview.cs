namespace StarWin.Application.Services.LegacyImport;

public sealed record StarWinLegacyImportSectorPreview(
    string SectorName,
    bool CanImport,
    IReadOnlyList<StarWinLegacyImportFileEntry> Files,
    IReadOnlyList<string> MissingRequiredExtensions,
    IReadOnlyList<string> MissingOptionalExtensions,
    int StarSystemRecordCount,
    IReadOnlyList<StarWinLegacyStarSystemPreview> SampleStarSystems,
    IReadOnlyList<string> Messages);
