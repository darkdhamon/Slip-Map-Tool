namespace StarWin.Domain.Model.Entity.Civilization;

public sealed class Colony
{
    public int Id { get; set; }

    public int WorldId { get; set; }

    public string Name { get; set; } = string.Empty;

    public WorldKind WorldKind { get; set; }

    public ushort RaceId { get; set; }

    public string ColonistRaceName { get; set; } = string.Empty;

    public ushort AllegianceId { get; set; } = ushort.MaxValue;

    public string AllegianceName { get; set; } = string.Empty;

    public ColonyPoliticalStatus PoliticalStatus { get; set; } = ColonyPoliticalStatus.Controlled;

    public int? ControllingEmpireId { get; set; }

    public int? ParentEmpireId { get; set; }

    public int? FoundingEmpireId { get; set; }

    public bool IsFoundedBy(int empireId)
    {
        return FoundingEmpireId == empireId || RaceId == empireId;
    }

    public bool IsControlledBy(int empireId)
    {
        return ControllingEmpireId == empireId || AllegianceId == empireId;
    }

    public ColonyEmpireRelationship GetRelationshipToEmpire(int empireId)
    {
        if (PoliticalStatus == ColonyPoliticalStatus.Independent || AllegianceId == ushort.MaxValue)
        {
            return IsFoundedBy(empireId) ? ColonyEmpireRelationship.Independent : ColonyEmpireRelationship.NotInvolved;
        }

        var foundedByEmpire = IsFoundedBy(empireId);
        var controlledByEmpire = IsControlledBy(empireId);

        return (foundedByEmpire, controlledByEmpire) switch
        {
            (true, true) => ColonyEmpireRelationship.Owned,
            (true, false) => ColonyEmpireRelationship.Captive,
            (false, true) => ColonyEmpireRelationship.Subjugated,
            _ => ColonyEmpireRelationship.NotInvolved
        };
    }

    public byte EncodedPopulation { get; set; }

    public long EstimatedPopulation { get; set; }

    public byte NativePopulationPercent { get; set; }

    public IList<ColonyDemographic> Demographics { get; } = new List<ColonyDemographic>();

    public string ColonyClass { get; set; } = string.Empty;

    public byte ColonyClassCode { get; set; }

    public byte Crime { get; set; }

    public byte Law { get; set; }

    public byte Stability { get; set; }

    public byte AgeCenturies { get; set; }

    public string Starport { get; set; } = string.Empty;

    public byte StarportCode { get; set; }

    public string GovernmentType { get; set; } = string.Empty;

    public byte GovernmentTypeCode { get; set; }

    public ushort GrossWorldProductMcr { get; set; }

    public ushort MilitaryPower { get; set; }

    public IList<string> Facilities { get; } = new List<string>();

    public bool HasLegacySpaceHabitatFacility =>
        Facilities.Any(facility => string.Equals(
            facility,
            ColonyFacilityNames.SpaceHabitats,
            StringComparison.OrdinalIgnoreCase));

    public string ExportResource { get; set; } = string.Empty;

    public byte ExportResourceCode { get; set; }

    public string ImportResource { get; set; } = string.Empty;

    public byte ImportResourceCode { get; set; }
}
