using StarWin.Domain.Model.ViewModel;

namespace StarWin.Domain.Services.Abstract;

public interface ICivilizationGenerator
{
    CivilizationGenerationResult Run(StarWinSector sector, CivilizationGeneratorSettings settings);
}
