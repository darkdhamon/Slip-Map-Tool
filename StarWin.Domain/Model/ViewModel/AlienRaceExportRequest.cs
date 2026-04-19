namespace StarWin.Domain.Model.ViewModel;

public sealed class AlienRaceExportRequest
{
    public required AlienRaceExportProfile Profile { get; init; }

    public AlienRaceExportFormat Format { get; init; } = AlienRaceExportFormat.PlainTextReport;

    public RolePlayingGameExportSystem GameSystem { get; init; } = RolePlayingGameExportSystem.None;

    public AlienRaceExportOutputFormat OutputFormat { get; init; } = AlienRaceExportOutputFormat.PlainText;
}
