using System.Globalization;

namespace StarWin.Domain.Services;

public static class GurpsTechnologyLevelMapper
{
    public static int GetBaseTechLevel(int starWinTechLevel)
    {
        return Math.Max(0, starWinTechLevel + 2);
    }

    public static string FormatDisplay(int baseTechLevel, bool isSuperscience)
    {
        return isSuperscience
            ? $"{baseTechLevel.ToString(CultureInfo.InvariantCulture)}^"
            : baseTechLevel.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatDisplay(byte starWinTechLevel, bool isSuperscience)
    {
        return FormatDisplay(GetBaseTechLevel(starWinTechLevel), isSuperscience);
    }

    public static bool TryParseDisplay(string? value, out int baseTechLevel, out bool isSuperscience)
    {
        baseTechLevel = 0;
        isSuperscience = false;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        isSuperscience = trimmed.EndsWith('^');
        var numericPortion = isSuperscience ? trimmed[..^1] : trimmed;
        return int.TryParse(numericPortion, NumberStyles.Integer, CultureInfo.InvariantCulture, out baseTechLevel);
    }
}
