namespace SlipMap.Domain.Model.ViewModel;

public sealed record DiceRoll(IReadOnlyList<int> Dice)
{
    public int Total => Dice.Sum();

    public override string ToString()
    {
        return $"{Total} ({string.Join(", ", Dice)})";
    }
}
