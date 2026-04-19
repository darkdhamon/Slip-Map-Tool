namespace StarWin.Application.Services.LegacyImport;

public sealed record StarWinLegacyImportFileEntry(
    string EntryName,
    string FileName,
    string SectorName,
    string Extension,
    long Length,
    bool IsRecognized);
