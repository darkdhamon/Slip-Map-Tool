using SlipMap.Domain.Model.ViewModel;

namespace SlipMap.Domain.Services.Abstract;

public interface IDiceRoller
{
    DiceRoll Roll(int sides, int count = 1);
}
