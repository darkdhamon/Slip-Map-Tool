namespace SlipMap.Domain.Model.ViewModel;

public sealed record NavigationJumpResult(
    NavigationJumpOutcome Outcome,
    int OriginSystemId,
    int DestinationSystemId,
    int ActualSystemId,
    string Message,
    DiceRoll? SkillRoll = null,
    int? TravelHours = null)
{
    public bool ShipMoved => OriginSystemId != ActualSystemId;
}
