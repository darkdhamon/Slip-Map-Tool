
namespace StarWin.Domain.Model.Entity.Legacy;

public sealed class StarWin2ColonyRecord
{
    public int WorldId { get; init; }

    public ushort RaceId { get; init; }

    public ushort AllegianceId { get; init; }

    public WorldKind WorldKind { get; init; }

    public byte EncodedPopulation { get; init; }

    public byte ColonyClass { get; init; }

    public byte Crime { get; init; }

    public byte Law { get; init; }

    public byte Stability { get; init; }

    public byte AgeCenturies { get; init; }

    public byte Starport { get; init; }

    public byte GovernmentType { get; init; }

    public ushort GrossNationalProductMcr { get; init; }

    public ushort Power { get; init; }

    public byte PopulationCompositionPercent { get; init; }

    public byte[] FacilityFlags { get; init; } = Array.Empty<byte>();

    public byte ExportResource { get; init; }

    public byte ImportResource { get; init; }
}
