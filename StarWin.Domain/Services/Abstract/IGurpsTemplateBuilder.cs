using StarWin.Domain.Model.ViewModel;

namespace StarWin.Domain.Services.Abstract;

public interface IGurpsTemplateBuilder
{
    GurpsTemplate Build(AlienRaceExportProfile profile, GurpsTemplateEdition edition);
}
