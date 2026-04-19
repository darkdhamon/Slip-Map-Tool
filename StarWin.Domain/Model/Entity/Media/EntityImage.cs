namespace StarWin.Domain.Model.Entity.Media;

public sealed class EntityImage
{
    public int Id { get; set; }

    public EntityImageTargetKind TargetKind { get; set; }

    public int TargetId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string Caption { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
