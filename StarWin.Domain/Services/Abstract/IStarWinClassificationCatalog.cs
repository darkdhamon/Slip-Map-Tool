
namespace StarWin.Domain.Services.Abstract;

public interface IStarWinClassificationCatalog
{
    AstralBodyKind GetAstralBodyKind(byte classificationCode);

    string GetAstralBodyClassification(byte classificationCode, byte decimalClassCode);

    string GetWorldType(byte worldTypeCode);

    string GetAtmosphereType(byte atmosphereTypeCode);

    string GetAtmosphereComposition(byte atmosphereCompositionCode);

    string GetWaterType(byte waterTypeCode);
}
