using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Infrastructure.Data;

namespace StarWin.Infrastructure.Services;

internal static class StarSystemNameUniqueness
{
    private static readonly string[] NatoAlphabet =
    [
        "Alpha",
        "Bravo",
        "Charlie",
        "Delta",
        "Echo",
        "Foxtrot",
        "Golf",
        "Hotel",
        "India",
        "Juliett",
        "Kilo",
        "Lima",
        "Mike",
        "November",
        "Oscar",
        "Papa",
        "Quebec",
        "Romeo",
        "Sierra",
        "Tango",
        "Uniform",
        "Victor",
        "Whiskey",
        "X-ray",
        "Yankee",
        "Zulu"
    ];

    public static async Task EnsureUniquePersistedNamesAsync(StarWinDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return;
        }

        List<StarSystem> systems;
        try
        {
            systems = await dbContext.StarSystems
                .OrderBy(system => system.SectorId)
                .ThenBy(system => system.LegacySystemId ?? int.MaxValue)
                .ThenBy(system => system.Id)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
        {
            return;
        }

        if (!EnsureUniqueNames(systems))
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static void EnsureUniqueImportedNames(IReadOnlyCollection<StarSystem> systems)
    {
        EnsureUniqueNames(systems);
    }

    public static async Task ValidateUniqueNameAsync(
        StarWinDbContext dbContext,
        int sectorId,
        string proposedName,
        int? excludedSystemId,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeSystemName(proposedName);
        var existingNames = await dbContext.StarSystems
            .Where(system => system.SectorId == sectorId
                && (excludedSystemId == null || system.Id != excludedSystemId.Value))
            .Select(system => system.Name)
            .ToListAsync(cancellationToken);

        if (existingNames.Any(existingName => string.Equals(existingName.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A system named '{normalizedName}' already exists in this sector. System names must be unique per sector.");
        }
    }

    private static bool EnsureUniqueNames(IEnumerable<StarSystem> systems)
    {
        var changed = false;

        foreach (var sectorSystems in systems.GroupBy(system => system.SectorId))
        {
            var usedNames = sectorSystems
                .Select(system => NormalizeSystemName(system.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var duplicateGroups = sectorSystems
                .GroupBy(system => NormalizeSystemName(system.Name), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .ToList();

            foreach (var duplicateGroup in duplicateGroups)
            {
                foreach (var system in duplicateGroup)
                {
                    usedNames.Remove(NormalizeSystemName(system.Name));
                }

                var baseName = BuildBaseDisplayName(duplicateGroup.Key);
                var suffixOrdinal = 0;

                foreach (var system in duplicateGroup
                    .OrderBy(item => item.LegacySystemId ?? int.MaxValue)
                    .ThenBy(item => item.Id))
                {
                    string candidateName;
                    do
                    {
                        suffixOrdinal++;
                        candidateName = $"{baseName} {GetNatoSuffix(suffixOrdinal)}";
                    }
                    while (!usedNames.Add(candidateName));

                    if (!string.Equals(system.Name, candidateName, StringComparison.Ordinal))
                    {
                        system.Name = candidateName;
                        changed = true;
                    }
                }
            }
        }

        return changed;
    }

    private static string NormalizeSystemName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "Unnamed System" : name.Trim();
    }

    private static string BuildBaseDisplayName(string normalizedName)
    {
        if (normalizedName.All(character => !char.IsLetter(character) || char.IsLower(character) || char.IsWhiteSpace(character)))
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalizedName.ToLowerInvariant());
        }

        return normalizedName;
    }

    private static string GetNatoSuffix(int ordinal)
    {
        var zeroBased = ordinal - 1;
        var word = NatoAlphabet[zeroBased % NatoAlphabet.Length];
        var cycle = zeroBased / NatoAlphabet.Length;

        return cycle == 0 ? word : $"{word} {cycle + 1}";
    }
}
