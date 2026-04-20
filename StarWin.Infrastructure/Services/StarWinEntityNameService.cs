using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Notes;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinEntityNameService(StarWinDbContext dbContext) : IStarWinEntityNameService
{
    public async Task<string> SaveNameAsync(
        EntityNoteTargetKind targetKind,
        int targetId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Name is required.");
        }

        switch (targetKind)
        {
            case EntityNoteTargetKind.StarSystem:
                var system = await dbContext.StarSystems.FirstOrDefaultAsync(item => item.Id == targetId, cancellationToken)
                    ?? throw new InvalidOperationException("System was not found.");
                system.Name = normalizedName;
                break;
            case EntityNoteTargetKind.World:
                var world = await dbContext.Worlds.FirstOrDefaultAsync(item => item.Id == targetId, cancellationToken)
                    ?? throw new InvalidOperationException("World was not found.");
                world.Name = normalizedName;
                break;
            case EntityNoteTargetKind.Colony:
                var colony = await dbContext.Colonies.FirstOrDefaultAsync(item => item.Id == targetId, cancellationToken)
                    ?? throw new InvalidOperationException("Colony was not found.");
                colony.Name = normalizedName;
                break;
            case EntityNoteTargetKind.AlienRace:
                var race = await dbContext.AlienRaces.FirstOrDefaultAsync(item => item.Id == targetId, cancellationToken)
                    ?? throw new InvalidOperationException("Race was not found.");
                race.Name = normalizedName;
                break;
            case EntityNoteTargetKind.Empire:
                var empire = await dbContext.Empires.FirstOrDefaultAsync(item => item.Id == targetId, cancellationToken)
                    ?? throw new InvalidOperationException("Empire was not found.");
                empire.Name = normalizedName;
                break;
            default:
                throw new InvalidOperationException("Unsupported name target.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return normalizedName;
    }
}
