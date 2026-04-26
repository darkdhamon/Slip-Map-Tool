using Microsoft.AspNetCore.WebUtilities;

namespace StarWin.Web.Components.Explorer;

public static class SectorExplorerRoutes
{
    public static string GetSectionNameFromSlug(string? sectionSlug)
    {
        if (string.IsNullOrWhiteSpace(sectionSlug))
        {
            return "Overview";
        }

        return sectionSlug.Trim().ToLowerInvariant() switch
        {
            "overview" => "Overview",
            "timeline" => "Timeline",
            "configuration" => "Configuration",
            "hyperlanes" => "Hyperlanes",
            "systems" => "Systems",
            "worlds" => "Worlds",
            "colonies" => "Colonies",
            "aliens" => "Aliens",
            "empires" => "Empires",
            _ => "Overview"
        };
    }

    public static string GetSectionSlug(string sectionName)
    {
        return sectionName switch
        {
            "Timeline" => "timeline",
            "Configuration" => "configuration",
            "Hyperlanes" => "hyperlanes",
            "Systems" => "systems",
            "Worlds" => "worlds",
            "Colonies" => "colonies",
            "Aliens" => "aliens",
            "Empires" => "empires",
            _ => "overview"
        };
    }

    public static string BuildSectionUri(
        string sectionName,
        int sectorId = 0,
        int systemId = 0,
        int worldId = 0,
        int colonyId = 0,
        int habitatId = 0,
        int raceId = 0,
        int empireId = 0)
    {
        var slug = GetSectionSlug(sectionName);
        var path = slug == "overview"
            ? "/sector-explorer"
            : $"/sector-explorer/{slug}";

        var query = new Dictionary<string, string?>();
        AddIfPositive(query, "sectorId", sectorId);
        AddIfPositive(query, "systemId", systemId);
        AddIfPositive(query, "worldId", worldId);
        AddIfPositive(query, "colonyId", colonyId);
        AddIfPositive(query, "habitatId", habitatId);
        AddIfPositive(query, "raceId", raceId);
        AddIfPositive(query, "empireId", empireId);

        return query.Count == 0 ? path : QueryHelpers.AddQueryString(path, query);
    }

    private static void AddIfPositive(IDictionary<string, string?> query, string key, int value)
    {
        if (value > 0)
        {
            query[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
