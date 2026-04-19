namespace StarWin.Application.Services.LegacyImport;

public interface IStarWinLegacyImportService
{
    Task<StarWinLegacyImportPreview> PreviewStarWinZipAsync(
        Stream zipPackage,
        string packageName,
        CancellationToken cancellationToken = default);

    Task<StarWinLegacyImportResult> ImportStarWinZipAsync(
        Stream zipPackage,
        string packageName,
        string targetSectorName,
        CancellationToken cancellationToken = default);
}
