using StarWin.Domain.Model.Entity.Media;

namespace StarWin.Application.Services;

public sealed record EntityImageTarget(EntityImageTargetKind TargetKind, int TargetId);

public interface IStarWinImageService
{
    Task<IReadOnlyList<EntityImage>> GetImagesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EntityImage>> GetImagesAsync(
        IReadOnlyCollection<EntityImageTarget> targets,
        CancellationToken cancellationToken = default)
        => targets.Count == 0
            ? Task.FromResult<IReadOnlyList<EntityImage>>([])
            : GetImagesAsync(cancellationToken);

    Task<EntityImage> UploadImageAsync(
        EntityImageTargetKind targetKind,
        int targetId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);
}
