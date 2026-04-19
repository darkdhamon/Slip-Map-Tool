namespace StarWin.Domain.Model.ViewModel;

public sealed class CivilizationGenerationLogOptions
{
    public bool WriteResearchPointLog { get; set; }

    public bool WriteEventLog { get; set; }

    public bool WriteEmpireHistoryLog { get; set; } = true;
}
