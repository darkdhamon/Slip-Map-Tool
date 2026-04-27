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
            Name = string.IsNullOrWhiteSpace(world.Name)
                ? $"{foundingRace.Name} Independent State"
                : $"{world.Name} Independent State",
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
