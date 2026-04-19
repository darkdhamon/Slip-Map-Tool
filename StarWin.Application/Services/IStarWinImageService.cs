using StarWin.Domain.Model.Entity.Media;

namespace StarWin.Application.Services;

public interface IStarWinImageService
{
    Task<IReadOnlyList<EntityImage>> GetImagesAsync(CancellationToken cancellationToken = default);

    Task<EntityImage> UploadImageAsync(
        EntityImageTargetKind targetKind,
        int targetId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);
}
