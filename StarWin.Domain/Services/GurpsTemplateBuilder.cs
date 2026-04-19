using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.ViewModel;
using StarWin.Domain.Services.Abstract;

namespace StarWin.Domain.Services;

public sealed class GurpsTemplateBuilder : IGurpsTemplateBuilder
{
    public GurpsTemplate Build(AlienRaceExportProfile profile, GurpsTemplateEdition edition)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var race = profile.Race;
        var template = new GurpsTemplate
        {
            Name = $"{race.Name} racial template",
            Edition = edition,
            Notes = edition == GurpsTemplateEdition.FourthEdition
                ? "Draft GURPS Fourth Edition racial template derived from StarWin biology and culture fields."
                : "Legacy StarWin-style racial template view."
        };

        AddAttributeModifiers(template, race);
        AddSecondaryCharacteristics(template, race);
        AddAdvantages(template, race, profile);
        AddDisadvantages(template, race);
        AddFeatures(template, race, profile);
        AddSkills(template, race);

        return template;
    }

    private static void AddAttributeModifiers(GurpsTemplate template, AlienRace race)
    {
        AddTrait(template.AttributeModifiers, "ST", AttributeDelta(race.BiologyProfile.Body), AttributeDelta(race.BiologyProfile.Body) * 10, "Derived from StarWin Body.");
        AddTrait(template.AttributeModifiers, "IQ", AttributeDelta(race.BiologyProfile.Mind), AttributeDelta(race.BiologyProfile.Mind) * 20, "Derived from StarWin Mind.");
        AddTrait(template.AttributeModifiers, "DX", AttributeDelta(race.BiologyProfile.Speed), AttributeDelta(race.BiologyProfile.Speed) * 20, "Derived from StarWin Speed.");

        if (race.MassKg >= 130)
        {
            AddTrait(template.AttributeModifiers, "HT", 1, 10, "Large, durable physiology.");
        }
        else if (race.MassKg <= 50)
        {
            AddTrait(template.AttributeModifiers, "ST", -1, -10, "Light physiology.");
        }
    }

    private static void AddSecondaryCharacteristics(GurpsTemplate template, AlienRace race)
    {
        if (race.BiologyProfile.Speed >= 12)
        {
            AddTrait(template.SecondaryCharacteristicModifiers, "Basic Move", 1, 5, "Fast StarWin Speed profile.");
        }

        if (race.BiologyProfile.Lifespan >= 36)
        {
            AddTrait(template.SecondaryCharacteristicModifiers, "Extended Lifespan", 1, 2, "Long-lived biology.");
        }
    }

    private static void AddAdvantages(GurpsTemplate template, AlienRace race, AlienRaceExportProfile profile)
    {
        if (race.BiologyProfile.PsiRating >= PsiPowerRating.Good)
        {
            var psiTalent = Math.Max(1, (int)race.BiologyProfile.PsiPower);
            AddTrait(template.Advantages, "Psi Talent", psiTalent, psiTalent * 5, $"StarWin psi rating: {race.BiologyProfile.PsiRating}.");
        }

        if (race.EnvironmentType.Contains("Vacuum", StringComparison.OrdinalIgnoreCase))
        {
            AddTrait(template.Advantages, "Vacuum Support", 1, 5, "Native or adapted vacuum environment.");
        }

        if (race.EnvironmentType.Contains("Aquatic", StringComparison.OrdinalIgnoreCase))
        {
            AddTrait(template.Advantages, "Amphibious", 1, 10, "Aquatic environment profile.");
        }

        if (race.AppearanceType.Contains("Avian", StringComparison.OrdinalIgnoreCase))
        {
            AddTrait(template.Advantages, "Flight", 1, 40, "Avian body plan.");
        }

        if (race.BodyCoverType.Contains("Hard", StringComparison.OrdinalIgnoreCase)
            || race.BodyCoverType.Contains("Crystal", StringComparison.OrdinalIgnoreCase)
            || race.BodyCoverType.Contains("Scales", StringComparison.OrdinalIgnoreCase))
        {
            AddTrait(template.Advantages, "Damage Resistance", 1, 5, $"Body cover: {race.BodyCoverType}.");
        }

        if (profile.Empire?.CivilizationProfile.TechLevel >= 9)
        {
            AddTrait(template.Advantages, "Cultural Familiarity", 1, 1, $"Spacefaring polity: {profile.Empire.Name}.");
        }
    }

    private static void AddDisadvantages(GurpsTemplate template, AlienRace race)
    {
        if (race.EnvironmentType.Contains("Subterranean", StringComparison.OrdinalIgnoreCase))
        {
            AddTrait(template.Disadvantages, "Light Sensitivity", 1, -10, "Subterranean environment profile.");
        }

        if (race.Diet.Contains("Mineral", StringComparison.OrdinalIgnoreCase)
            || race.Diet.Contains("Energy", StringComparison.OrdinalIgnoreCase))
        {
            AddTrait(template.Disadvantages, "Restricted Diet", 1, -10, $"Diet: {race.Diet}.");
        }

        if (race.BiologyProfile.PsiRating is PsiPowerRating.VeryPoor or PsiPowerRating.Poor)
        {
            AddTrait(template.Disadvantages, "Low Psi Potential", 1, -5, $"StarWin psi rating: {race.BiologyProfile.PsiRating}.");
        }
    }

    private static void AddFeatures(GurpsTemplate template, AlienRace race, AlienRaceExportProfile profile)
    {
        AddTrait(template.Features, "Body chemistry", 0, 0, race.BodyChemistry);
        AddTrait(template.Features, "Appearance", 0, 0, race.AppearanceType);
        AddTrait(template.Features, "Reproduction", 0, 0, race.ReproductionMethod);
        AddTrait(template.Features, "Limb pairs", race.LimbPairCount, 0, $"{race.LimbPairCount} paired limb groups.");

        if (profile.HomeWorld is not null)
        {
            AddTrait(template.Features, "Native world", 0, 0, $"{profile.HomeWorld.Name}, {profile.HomeWorld.WorldType}.");
        }
    }

    private static void AddSkills(GurpsTemplate template, AlienRace race)
    {
        if (race.EnvironmentType.Contains("Aquatic", StringComparison.OrdinalIgnoreCase))
        {
            AddTrait(template.Skills, "Swimming", 1, 1, "Native environment adaptation.");
        }

        if (race.EnvironmentType.Contains("Aerial", StringComparison.OrdinalIgnoreCase))
        {
            AddTrait(template.Skills, "Flight", 1, 2, "Aerial environment adaptation.");
        }

        if (race.BiologyProfile.Mind >= 12)
        {
            AddTrait(template.Skills, "Research", 1, 2, "High StarWin Mind profile.");
        }
    }

    private static int AttributeDelta(byte score)
    {
        return Math.Clamp((score - 10) / 2, -3, 3);
    }

    private static void AddTrait(IList<GurpsTemplateTrait> traits, string name, int level, int pointCost, string notes)
    {
        if (pointCost == 0 && traits.Any(trait => string.Equals(trait.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        traits.Add(new GurpsTemplateTrait
        {
            Name = name,
            Level = level,
            PointCost = pointCost,
            Notes = notes
        });
    }
}
