using StarWin.Domain.Model.ViewModel;

namespace StarWin.Domain.Services.Abstract;

public interface IArmyGenerator
{
    ArmyGenerationResult Generate(ArmyGeneratorSettings settings);

    ArmyGenerationResult GenerateForEmpire(Empire empire, ArmyGeneratorSettings settings);
}
