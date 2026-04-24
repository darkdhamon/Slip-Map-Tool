using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinEntityNoteService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinEntityNoteService
{
    public async Task<EntityNote?> GetNoteAsync(
        EntityNoteTargetKind targetKind,
        int targetId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.EntityNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                note => note.TargetKind == targetKind && note.TargetId == targetId,
                cancellationToken);
    }

    public async Task<EntityNote?> SaveNoteAsync(
        EntityNoteTargetKind targetKind,
        int targetId,
        string markdown,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedMarkdown = markdown.Trim();
        var existingNote = await dbContext.EntityNotes
            .FirstOrDefaultAsync(
                note => note.TargetKind == targetKind && note.TargetId == targetId,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(normalizedMarkdown))
        {
            if (existingNote is not null)
            {
                dbContext.EntityNotes.Remove(existingNote);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        if (existingNote is null)
        {
            existingNote = new EntityNote
            {
                TargetKind = targetKind,
                TargetId = targetId
            };
            dbContext.EntityNotes.Add(existingNote);
        }

        existingNote.Markdown = normalizedMarkdown;
        existingNote.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return existingNote;
    }
}
