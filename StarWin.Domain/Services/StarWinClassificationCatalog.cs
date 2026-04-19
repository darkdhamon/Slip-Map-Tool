using StarWin.Domain.Services.Abstract;

namespace StarWin.Domain.Services;

public sealed class StarWinClassificationCatalog : IStarWinClassificationCatalog
{
    private static readonly string[] AstralClasses =
    [
        "A", "M", "F", "F", "G", "K", "M", "M", "F", "M", "B", "M", "O", string.Empty,
        "Nebula", "Pulsar", "Black hole", "Quasar", "Ion storms", "Primary Star Characteristics",
        "Companion Star Characteristics", "Space Rift"
    ];

    public AstralBodyKind GetAstralBodyKind(byte classificationCode)
    {
        return classificationCode switch
        {
            >= 1 and <= 13 => AstralBodyKind.Star,
            15 => AstralBodyKind.Nebula,
            16 => AstralBodyKind.Pulsar,
            17 => AstralBodyKind.BlackHole,
            18 => AstralBodyKind.Quasar,
            19 => AstralBodyKind.IonStorm,
            22 => AstralBodyKind.SpaceRift,
            _ => AstralBodyKind.Unknown
        };
    }

    public string GetAstralBodyClassification(byte classificationCode, byte decimalClassCode)
    {
        if (classificationCode < 1 || classificationCode > AstralClasses.Length)
        {
            return string.Empty;
        }

        var classification = AstralClasses[classificationCode - 1];
        if (classificationCode >= 14)
        {
            return classification;
        }

        return classificationCode switch
        {
            1 or 11 => $"{classification}{decimalClassCode} II",
            2 => $"{classification}{decimalClassCode} III",
            3 => $"{classification}{decimalClassCode} IV",
            >= 4 and <= 7 => $"{classification}{decimalClassCode} V",
            8 => $"{classification}{decimalClassCode} VI",
            9 => $"{classification}{decimalClassCode} VII",
            10 => $"{classification}{decimalClassCode} Ib",
            12 or 13 => $"{classification}{decimalClassCode} Ia",
            _ => $"{classification}{decimalClassCode}"
        };
    }

    public string GetWorldType(byte worldTypeCode)
    {
        return worldTypeCode.ToString();
    }

    public string GetAtmosphereType(byte atmosphereTypeCode)
    {
        return atmosphereTypeCode.ToString();
    }

    public string GetAtmosphereComposition(byte atmosphereCompositionCode)
    {
        return atmosphereCompositionCode.ToString();
    }

    public string GetWaterType(byte waterTypeCode)
    {
        return waterTypeCode.ToString();
    }
}
