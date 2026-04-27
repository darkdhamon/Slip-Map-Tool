namespace StarWin.Application.Services;

public interface IStarWinAppConfigurationService
{
    Task ResetDatabaseAsync(CancellationToken cancellationToken = default);
}
