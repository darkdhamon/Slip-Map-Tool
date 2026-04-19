namespace StarWin.Domain.Model.ViewModel;

public sealed class GurpsTemplate
{
    public string Name { get; set; } = string.Empty;

    public GurpsTemplateEdition Edition { get; set; } = GurpsTemplateEdition.FourthEdition;

    public int TotalPointCost =>
        AttributeModifiers.Sum(modifier => modifier.PointCost)
        + SecondaryCharacteristicModifiers.Sum(modifier => modifier.PointCost)
        + Advantages.Sum(trait => trait.PointCost)
        + Disadvantages.Sum(trait => trait.PointCost)
        + Quirks.Sum(trait => trait.PointCost)
        + Skills.Sum(trait => trait.PointCost);

    public IList<GurpsTemplateTrait> AttributeModifiers { get; } = new List<GurpsTemplateTrait>();

    public IList<GurpsTemplateTrait> SecondaryCharacteristicModifiers { get; } = new List<GurpsTemplateTrait>();

    public IList<GurpsTemplateTrait> Advantages { get; } = new List<GurpsTemplateTrait>();

    public IList<GurpsTemplateTrait> Disadvantages { get; } = new List<GurpsTemplateTrait>();

    public IList<GurpsTemplateTrait> Quirks { get; } = new List<GurpsTemplateTrait>();

    public IList<GurpsTemplateTrait> Features { get; } = new List<GurpsTemplateTrait>();

    public IList<GurpsTemplateTrait> Skills { get; } = new List<GurpsTemplateTrait>();

    public string Notes { get; set; } = string.Empty;
}
