using SlipMap.Domain.Model.ViewModel;
using SlipMap.Domain.Services.Abstract;

namespace SlipMap.Domain.Services;

public sealed class RandomDiceRoller : IDiceRoller
{
    private readonly IRandomNumberGenerator _random;

    public RandomDiceRoller(IRandomNumberGenerator? random = null)
    {
        _random = random ?? new SystemRandomNumberGenerator();
    }

    public DiceRoll Roll(int sides, int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var dice = new int[count];
        for (var i = 0; i < dice.Length; i++)
        {
            dice[i] = _random.Next(1, sides + 1);
        }

        return new DiceRoll(dice);
    }
}
