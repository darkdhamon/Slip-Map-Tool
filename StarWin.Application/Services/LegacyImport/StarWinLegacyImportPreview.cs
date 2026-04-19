namespace StarWin.Application.Services.LegacyImport;

public sealed record StarWinLegacyImportPreview(
    string PackageName,
    IReadOnlyList<StarWinLegacyImportSectorPreview> Sectors,
    IReadOnlyList<StarWinLegacyImportFileEntry> UnmatchedFiles,
    IReadOnlyList<string> Messages)
{
    public bool CanImport => Sectors.Count > 0 && Sectors.All(sector => sector.CanImport);
}
