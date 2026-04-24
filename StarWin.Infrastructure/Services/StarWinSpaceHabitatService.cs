using Microsoft.EntityFrameworkCore;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

public sealed class StarWinSpaceHabitatService(IDbContextFactory<StarWinDbContext> dbContextFactory) : IStarWinSpaceHabitatService
{
    public async Task<SpaceHabitat> CreateOrbitingAstralBodyAsync(
        int starSystemId,
        int astralBodySequence,
        int empireId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var system = await dbContext.StarSystems
            .Include(item => item.AstralBodies)
            .FirstOrDefaultAsync(item => item.Id == starSystemId, cancellationToken)
            ?? throw new InvalidOperationException("System was not found.");

        if (astralBodySequence < 0 || astralBodySequence >= system.AstralBodies.Count)
        {
            throw new InvalidOperationException("Astral body was not found.");
        }

        var empire = await dbContext.Empires.FirstOrDefaultAsync(item => item.Id == empireId, cancellationToken)
            ?? throw new InvalidOperationException("Empire was not found.");
        var body = system.AstralBodies.ElementAt(astralBodySequence);
        var habitat = new SpaceHabitat
        {
            Id = await GetNextSpaceHabitatIdAsync(dbContext, cancellationToken),
            Name = NormalizeHabitatName(name, $"{body.Role} Habitat"),
            OrbitTargetKind = OrbitTargetKind.AstralBody,
            OrbitTargetId = astralBodySequence,
            BuiltByEmpireId = empire.Id,
            ControlledByEmpireId = empire.Id
        };

        dbContext.SpaceHabitats.Add(habitat);
        dbContext.Entry(habitat).Property("StarSystemId").CurrentValue = system.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        return habitat;
    }

    public async Task<SpaceHabitat> CreateOrbitingWorldAsync(
        int worldId,
        int empireId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var world = await dbContext.Worlds.FirstOrDefaultAsync(item => item.Id == worldId, cancellationToken)
            ?? throw new InvalidOperationException("World was not found.");
        if (world.StarSystemId is null)
        {
            throw new InvalidOperationException("World is not assigned to a system.");
        }

        var empire = await dbContext.Empires.FirstOrDefaultAsync(item => item.Id == empireId, cancellationToken)
            ?? throw new InvalidOperationException("Empire was not found.");
        var habitat = new SpaceHabitat
        {
            Id = await GetNextSpaceHabitatIdAsync(dbContext, cancellationToken),
            Name = NormalizeHabitatName(name, $"{world.Name} Habitat"),
            OrbitTargetKind = OrbitTargetKind.World,
            OrbitTargetId = world.Id,
            BuiltByEmpireId = empire.Id,
            ControlledByEmpireId = empire.Id
        };

        dbContext.SpaceHabitats.Add(habitat);
        dbContext.Entry(habitat).Property("StarSystemId").CurrentValue = world.StarSystemId.Value;
        await dbContext.SaveChangesAsync(cancellationToken);
        return habitat;
    }

    private async Task<int> GetNextSpaceHabitatIdAsync(StarWinDbContext dbContext, CancellationToken cancellationToken)
    {
        return (await dbContext.SpaceHabitats
            .Select(habitat => (int?)habitat.Id)
            .MaxAsync(cancellationToken) ?? 0) + 1;
    }

    private static string NormalizeHabitatName(string? name, string fallback)
    {
        return string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
    }
}
