using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Application.Services;

public sealed class StarWinSearchService(IStarWinWorkspace workspace) : IStarWinSearchService
{
    public IReadOnlyList<StarWinSearchResult> Search(string query, int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var results = new List<StarWinSearchResult>();
        var importIdQuery = ParseImportIdQuery(query);
        foreach (var sector in workspace.Sectors)
        {
            foreach (var system in sector.Systems)
            {
                if (Matches(system.Name, query)
                    || Matches(system.Id.ToString(), query)
                    || Matches(system.LegacySystemId?.ToString(), query)
                    || MatchesImportId(importIdQuery, "SID", system.LegacySystemId)
                    || Matches(sector.Name, query))
                {
                    results.Add(new StarWinSearchResult
                    {
                        Type = StarWinSearchResultType.StarSystem,
                        Title = system.Name,
                        Subtitle = $"{sector.Name} system {DisplayImportId("SID", system.LegacySystemId, system.Id)} at {DisplayCoordinates(system.Coordinates)}",
                        Tab = "Systems",
                        SectorId = sector.Id,
                        SystemId = system.Id
                    });
                }

                foreach (var world in system.Worlds)
                {
                    var legacyWorldPrefix = world.Kind == WorldKind.Moon ? "MID" : "PID";
                    var legacyWorldId = world.Kind == WorldKind.Moon ? world.LegacyMoonId : world.LegacyPlanetId;
                    if (Matches(world.Name, query)
                        || Matches(world.WorldType, query)
                        || Matches(world.Id.ToString(), query)
                        || Matches(legacyWorldId?.ToString(), query)
                        || MatchesImportId(importIdQuery, legacyWorldPrefix, legacyWorldId))
                    {
                        results.Add(new StarWinSearchResult
                        {
                            Type = StarWinSearchResultType.World,
                            Title = world.Name,
                            Subtitle = $"{world.Kind} {DisplayImportId(legacyWorldPrefix, legacyWorldId, world.Id)} in {system.Name}, {sector.Name}; {world.WorldType}, {world.AtmosphereType}",
                            Tab = "Worlds",
                            SectorId = sector.Id,
                            SystemId = system.Id,
                            WorldId = world.Id
                        });
                    }

                    if (world.Colony is not null
                        && (Matches(world.Colony.ColonyClass, query)
                            || Matches(world.Colony.ColonistRaceName, query)
                            || Matches(world.Colony.AllegianceName, query)
                            || Matches(world.Colony.Id.ToString(), query)))
                    {
                        results.Add(new StarWinSearchResult
                        {
                            Type = StarWinSearchResultType.Colony,
                            Title = $"{world.Colony.ColonyClass} on {world.Name}",
                            Subtitle = $"{world.Colony.ColonistRaceName}; {world.Colony.AllegianceName}; {DisplayPopulation(world.Colony.EstimatedPopulation)}",
                            Tab = "Colonies",
                            SectorId = sector.Id,
                            SystemId = system.Id,
                            WorldId = world.Id,
                            ColonyId = world.Colony.Id,
                            RaceId = world.Colony.RaceId,
                            EmpireId = world.Colony.ControllingEmpireId
                        });
                    }
                }

                foreach (var habitat in system.SpaceHabitats)
                {
                    if (Matches(habitat.Name, query) || Matches(habitat.Id.ToString(), query))
                    {
                        results.Add(new StarWinSearchResult
                        {
                            Type = StarWinSearchResultType.SpaceHabitat,
                            Title = habitat.Name,
                            Subtitle = $"Habitat in {system.Name}, {sector.Name}; population {DisplayPopulation(habitat.Population)}",
                            Tab = "Systems",
                            SectorId = sector.Id,
                            SystemId = system.Id,
                            SpaceHabitatId = habitat.Id,
                            EmpireId = habitat.ControlledByEmpireId
                        });
                    }
                }
            }

            foreach (var history in sector.History)
            {
                if (Matches(history.EventType, query) || Matches(history.Description, query) || Matches(history.Century.ToString(), query))
                {
                    results.Add(new StarWinSearchResult
                    {
                        Type = StarWinSearchResultType.History,
                        Title = $"{history.EventType} C{history.Century}",
                        Subtitle = $"{sector.Name}; {history.Description}",
                        Tab = "Systems",
                        SectorId = sector.Id,
                        SystemId = history.StarSystemId,
                        WorldId = history.PlanetId,
                        RaceId = history.RaceId
                    });
                }
            }
        }

        foreach (var empire in workspace.Empires)
        {
            if (Matches(empire.Name, query)
                || Matches(empire.Id.ToString(), query)
                || Matches(empire.CivilizationProfile.TechLevel.ToString(), query))
            {
                results.Add(new StarWinSearchResult
                {
                    Type = StarWinSearchResultType.Empire,
                    Title = empire.Name,
                    Subtitle = $"TL {empire.CivilizationProfile.TechLevel}; {empire.Planets} planets; {empire.MilitaryPower} military power",
                    Tab = "Empires",
                    EmpireId = empire.Id,
                    RaceId = empire.LegacyRaceId
                });
            }
        }

        foreach (var race in workspace.AlienRaces)
        {
            if (Matches(race.Name, query)
                || Matches(race.AppearanceType, query)
                || Matches(race.EnvironmentType, query)
                || Matches(race.BodyChemistry, query)
                || Matches(race.Id.ToString(), query))
            {
                results.Add(new StarWinSearchResult
                {
                    Type = StarWinSearchResultType.AlienRace,
                    Title = race.Name,
                    Subtitle = $"{race.AppearanceType}; {race.EnvironmentType}; {race.BodyChemistry} biology",
                    Tab = "Aliens",
                    RaceId = race.Id
                });
            }
        }

        return results
            .OrderBy(result => Rank(result, query))
            .ThenBy(result => result.Title)
            .Take(maxResults)
            .ToList();
    }

    private static bool Matches(string? value, string query)
    {
        return value?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool MatchesImportId(ImportIdQuery? importIdQuery, string prefix, int? legacyId)
    {
        return importIdQuery is not null
            && legacyId is not null
            && string.Equals(importIdQuery.Prefix, prefix, StringComparison.OrdinalIgnoreCase)
            && importIdQuery.Id == legacyId.Value;
    }

    private static ImportIdQuery? ParseImportIdQuery(string query)
    {
        var normalized = query.Trim();
        if (normalized.Length < 2)
        {
            return null;
        }

        foreach (var prefix in new[] { "SID", "PID", "MID" })
        {
            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var idPart = normalized[prefix.Length..].TrimStart(' ', ':', '#', '-');
            return int.TryParse(idPart, out var id)
                ? new ImportIdQuery(prefix, id)
                : null;
        }

        return null;
    }

    private static string DisplayImportId(string prefix, int? legacyId, int entityId)
    {
        return legacyId is null ? entityId.ToString() : $"{prefix} {legacyId}";
    }

    private static string DisplayCoordinates(Coordinates coordinates)
    {
        return $"{coordinates.XParsecs:0.#}, {coordinates.YParsecs:0.#}, {coordinates.ZParsecs:0.#}";
    }

    private static int Rank(StarWinSearchResult result, string query)
    {
        if (string.Equals(result.Title, query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (result.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return result.Type switch
        {
            StarWinSearchResultType.StarSystem => 2,
            StarWinSearchResultType.World => 3,
            StarWinSearchResultType.AlienRace => 4,
            StarWinSearchResultType.Colony => 5,
            StarWinSearchResultType.Empire => 6,
            _ => 7
        };
    }

    private static string DisplayPopulation(long population)
    {
        return population >= 1_000_000_000
            ? $"{population / 1_000_000_000m:0.#} billion"
            : $"{population / 1_000_000m:0.#} million";
    }

    private sealed record ImportIdQuery(string Prefix, int Id);
}
