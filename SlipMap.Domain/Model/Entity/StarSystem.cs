namespace SlipMap.Domain.Model.Entity;

public sealed class StarSystem
{
    public StarSystem(int id, string? name = null, string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);

        Id = id;
        UpdateDetails(name, notes);
    }

    public int Id { get; }

    public string? Name { get; private set; }

    public string? Notes { get; private set; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"System {Id}" : $"{Name} ({Id})";

    public void Rename(string? name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void UpdateDetails(string? name, string? notes)
    {
        if (name is not null)
        {
            Rename(name);
        }

        if (notes is not null)
        {
            UpdateNotes(notes);
        }
    }

    public override string ToString()
    {
        return DisplayName;
    }
}
