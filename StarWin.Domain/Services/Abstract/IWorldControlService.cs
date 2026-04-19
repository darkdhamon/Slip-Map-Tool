
namespace StarWin.Domain.Services.Abstract;

public interface IWorldControlService
{
    void TransferControl(World world, Empire controllingEmpire);

    void ClearControl(World world);
}
