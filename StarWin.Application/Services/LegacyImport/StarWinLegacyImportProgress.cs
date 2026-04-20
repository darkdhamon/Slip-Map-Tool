namespace StarWin.Application.Services.LegacyImport;

public sealed record StarWinLegacyImportProgress(
    int PercentComplete,
    string Status,
    string Detail);
