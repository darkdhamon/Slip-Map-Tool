namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class Empire
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? LegacyRaceId { get; set; }

    public CivilizationProfile CivilizationProfile { get; set; } = new();

    public EmpireFounding Founding { get; set; } = new();

    public EmpireExpansionPolicy ExpansionPolicy { get; set; } = EmpireExpansionPolicy.CanExpand;

    public long EconomicPowerMcr { get; set; }

    public long MilitaryPower { get; set; }

    public long TradeBonusMcr { get; set; }

    public int Planets { get; set; }

    public int CaptivePlanets { get; set; }

    public int Moons { get; set; }

    public int SubjugatedPlanets { get; set; }

    public int SubjugatedMoons { get; set; }

    public int IndependentColonies { get; set; }

    public int SpaceHabitats { get; set; }

    public long NativePopulationMillions { get; set; }

    public long CaptivePopulationMillions { get; set; }

    public long SubjectPopulationMillions { get; set; }

    public long IndependentPopulationMillions { get; set; }

    public MilitaryForceProfile MilitaryForces { get; set; } = new();

    public IList<EmpireRaceMembership> RaceMemberships { get; } = new List<EmpireRaceMembership>();

    public IList<EmpireContact> Contacts { get; } = new List<EmpireContact>();
}
