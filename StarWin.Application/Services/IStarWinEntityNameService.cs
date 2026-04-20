using StarWin.Domain.Model.Entity.Notes;

namespace StarWin.Application.Services;

public interface IStarWinEntityNameService
{
    Task<string> SaveNameAsync(
        EntityNoteTargetKind targetKind,
        int targetId,
        string name,
        CancellationToken cancellationToken = default);
}
