namespace StarWin.Domain.Model.Entity.StarMap;

public sealed class SectorSavedRoute
{
    public int Id { get; set; }

    public int SectorId { get; set; }

    public int SourceSystemId { get; set; }

    public int TargetSystemId { get; set; }

    public decimal DistanceParsecs { get; set; }

    public decimal TravelTimeYears { get; set; }

    public byte TechnologyLevel { get; set; }

    public string TierName { get; set; } = string.Empty;

    public int? PrimaryOwnerEmpireId { get; set; }

    public string PrimaryOwnerEmpireName { get; set; } = string.Empty;

    public int? SecondaryOwnerEmpireId { get; set; }

    public string SecondaryOwnerEmpireName { get; set; } = string.Empty;

    public bool IsUserPersisted { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
