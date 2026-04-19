
namespace StarWin.Domain.Model.ViewModel;

public sealed class ArmyGenerationResult
{
    public required ArmyGeneratorSettings Settings { get; init; }

    public required MilitaryForceProfile MilitaryForces { get; init; }

    public string ReportText { get; init; } = string.Empty;
}
