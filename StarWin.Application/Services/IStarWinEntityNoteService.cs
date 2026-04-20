using StarWin.Domain.Model.Entity.Notes;

namespace StarWin.Application.Services;

public interface IStarWinEntityNoteService
{
    Task<EntityNote?> GetNoteAsync(
        EntityNoteTargetKind targetKind,
        int targetId,
        CancellationToken cancellationToken = default);

    Task<EntityNote?> SaveNoteAsync(
        EntityNoteTargetKind targetKind,
        int targetId,
        string markdown,
        CancellationToken cancellationToken = default);
}
