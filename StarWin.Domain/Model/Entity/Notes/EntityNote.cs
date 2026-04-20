namespace StarWin.Domain.Model.Entity.Notes;

public sealed class EntityNote
{
    public int Id { get; set; }

    public EntityNoteTargetKind TargetKind { get; set; }

    public int TargetId { get; set; }

    public string Markdown { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
