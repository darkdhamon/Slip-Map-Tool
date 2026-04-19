using StarWin.Domain.Model.ViewModel;

namespace StarWin.Domain.Services.Abstract;

public interface IAlienRaceExporter
{
    AlienRaceExportDocument Export(AlienRaceExportRequest request);
}
