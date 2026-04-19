namespace StarWin.Application.Services.LegacyImport;

public sealed record StarWinLegacyImportResult(
    bool Success,
    int? SectorId,
    StarWinLegacyImportPreview Preview,
    IReadOnlyList<string> Messages);
