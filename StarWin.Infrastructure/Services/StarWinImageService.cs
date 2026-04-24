using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Media;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinImageService(StarWinDbContext dbContext, IHostEnvironment environment) : IStarWinImageService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    public async Task<IReadOnlyList<EntityImage>> GetImagesAsync(CancellationToken cancellationToken = default)
    {
        var images = await dbContext.EntityImages
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // SQLite cannot order by DateTimeOffset server-side, so normalize ordering in memory.
        return images
            .OrderBy(image => image.TargetKind)
            .ThenBy(image => image.TargetId)
            .ThenByDescending(image => image.IsPrimary)
            .ThenBy(image => image.UploadedAt.UtcTicks)
            .ToList();
    }

    public async Task<EntityImage> UploadImageAsync(
        EntityImageTargetKind targetKind,
        int targetId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Only JPEG, PNG, GIF, and WebP images can be uploaded.");
        }

        var uploadsDirectory = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "starwin-images");
        Directory.CreateDirectory(uploadsDirectory);

        if (!AllowsMultipleImages(targetKind))
        {
            var existingImages = await dbContext.EntityImages
                .Where(image => image.TargetKind == targetKind && image.TargetId == targetId)
                .ToListAsync(cancellationToken);

            foreach (var existingImage in existingImages)
            {
                TryDeletePhysicalFile(existingImage.RelativePath);
            }

            dbContext.EntityImages.RemoveRange(existingImages);
        }

        var extension = Path.GetExtension(fileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".img" : extension.ToLowerInvariant();
        var storedFileName = $"{targetKind.ToString().ToLowerInvariant()}-{targetId}-{Guid.NewGuid():N}{safeExtension}";
        var physicalPath = Path.Combine(uploadsDirectory, storedFileName);

        await using (var fileStream = File.Create(physicalPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var hasPrimaryImage = await dbContext.EntityImages
            .AnyAsync(image => image.TargetKind == targetKind && image.TargetId == targetId && image.IsPrimary, cancellationToken);

        var entityImage = new EntityImage
        {
            TargetKind = targetKind,
            TargetId = targetId,
            FileName = Path.GetFileName(fileName),
            ContentType = contentType,
            RelativePath = $"/uploads/starwin-images/{storedFileName}",
            IsPrimary = !hasPrimaryImage,
            UploadedAt = DateTimeOffset.UtcNow
        };

        dbContext.EntityImages.Add(entityImage);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entityImage;
    }

    private static bool AllowsMultipleImages(EntityImageTargetKind targetKind)
    {
        return targetKind is EntityImageTargetKind.Colony or EntityImageTargetKind.AlienRace;
    }

    private void TryDeletePhysicalFile(string relativePath)
    {
        var rootedRelativePath = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(environment.ContentRootPath, "wwwroot", rootedRelativePath);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }
}
