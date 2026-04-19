namespace StarWin.Domain.Model.ViewModel;

public sealed class AlienRaceExportDocument
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "text/plain";

    public string Content { get; init; } = string.Empty;
}
