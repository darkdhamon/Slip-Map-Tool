using StarWin.Domain.Services.Abstract;

namespace StarWin.Domain.Services;

public sealed class IndependentColonyEmpireFactory(Func<double>? nextRandomValue = null) : IIndependentColonyEmpireFactory
{
    private const int CivilizationTraitMinimum = 0;
    private const int CivilizationTraitMaximum = 20;
    private readonly Func<double> nextRandomValue = nextRandomValue ?? Random.Shared.NextDouble;

    public Empire CreateEmpireFromIndependentColony(Colony colony, World world, AlienRace foundingRace, Empire? parentEmpire)
    {
        ArgumentNullException.ThrowIfNull(colony);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(foundingRace);

        var empire = new Empire
        {
            Name = BuildIndependentEmpireName(world, foundingRace, parentEmpire),
            LegacyRaceId = foundingRace.Id,
            ExpansionPolicy = EmpireExpansionPolicy.CanExpand,
            Founding =
            {
                Origin = EmpireOrigin.IndependentColony,
                FoundingWorldId = world.Id,
                FoundingColonyId = colony.Id,
                FoundingRaceId = foundingRace.Id,
                ParentEmpireId = parentEmpire?.Id
            },
            EconomicPowerMcr = colony.GrossWorldProductMcr,
            MilitaryPower = colony.MilitaryPower,
            IndependentPopulationMillions = colony.EstimatedPopulation / 1_000_000
        };

        empire.CivilizationModifiers = new CivilizationModifierProfile
        {
            Militancy = RollModifier(),
            Determination = RollModifier(),
            RacialTolerance = RollModifier(),
            Progressiveness = RollModifier(),
            Loyalty = RollModifier(),
            SocialCohesion = RollModifier(),
            Art = RollModifier(),
            Individualism = RollModifier()
        };

        empire.CivilizationProfile = new CivilizationProfile
        {
            Militancy = ApplyModifier(foundingRace.CivilizationProfile.Militancy, empire.CivilizationModifiers.Militancy),
            Determination = ApplyModifier(foundingRace.CivilizationProfile.Determination, empire.CivilizationModifiers.Determination),
            RacialTolerance = ApplyModifier(foundingRace.CivilizationProfile.RacialTolerance, empire.CivilizationModifiers.RacialTolerance),
            Progressiveness = ApplyModifier(foundingRace.CivilizationProfile.Progressiveness, empire.CivilizationModifiers.Progressiveness),
            Loyalty = ApplyModifier(foundingRace.CivilizationProfile.Loyalty, empire.CivilizationModifiers.Loyalty),
            SocialCohesion = ApplyModifier(foundingRace.CivilizationProfile.SocialCohesion, empire.CivilizationModifiers.SocialCohesion),
            TechLevel = parentEmpire?.CivilizationProfile.TechLevel ?? 0,
            Art = ApplyModifier(foundingRace.CivilizationProfile.Art, empire.CivilizationModifiers.Art),
            Individualism = ApplyModifier(foundingRace.CivilizationProfile.Individualism, empire.CivilizationModifiers.Individualism),
            SpatialAge = parentEmpire?.CivilizationProfile.SpatialAge ?? 0
        };

        empire.RaceMemberships.Add(new EmpireRaceMembership
        {
            RaceId = foundingRace.Id,
            Role = EmpireRaceRole.Founder,
            PopulationMillions = colony.EstimatedPopulation / 1_000_000,
            IsPrimary = true
        });

        return empire;
    }

    private static string BuildIndependentEmpireName(World world, AlienRace foundingRace, Empire? parentEmpire)
    {
        if (!string.IsNullOrWhiteSpace(foundingRace.Name)
            && !foundingRace.Name.Trim().StartsWith("Race ", StringComparison.OrdinalIgnoreCase))
        {
            return $"{foundingRace.Name.Trim()} Independent State";
        }

        if (!string.IsNullOrWhiteSpace(parentEmpire?.Name)
            && !parentEmpire.Name.Trim().StartsWith("Empire ", StringComparison.OrdinalIgnoreCase))
        {
            return parentEmpire.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(world.Name))
        {
            return $"{world.Name.Trim()} Independent State";
        }

        return $"Empire {foundingRace.Id}";
    }

    private int RollModifier()
    {
        var roll = nextRandomValue();
        return roll switch
        {
            < 0.50 => 0,
            < 0.625 => 1,
            < 0.75 => -1,
            < 0.825 => 2,
            < 0.90 => -2,
            < 0.95 => 3,
            _ => -3
        };
    }

    private static byte ApplyModifier(byte baseline, int modifier)
    {
        return (byte)Math.Clamp(baseline + modifier, CivilizationTraitMinimum, CivilizationTraitMaximum);
    }
}
