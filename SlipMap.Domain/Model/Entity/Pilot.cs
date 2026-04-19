namespace SlipMap.Domain.Model.Entity;

public sealed class Pilot
{
    public Pilot(string name, int skillLevel, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A pilot must have a name.", nameof(name));
        }

        ValidateSkillLevel(skillLevel);

        Id = id ?? Guid.NewGuid();
        Name = name.Trim();
        SkillLevel = skillLevel;
        IsActive = true;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public int SkillLevel { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A pilot must have a name.", nameof(name));
        }

        Name = name.Trim();
    }

    public void SetSkillLevel(int skillLevel)
    {
        ValidateSkillLevel(skillLevel);
        SkillLevel = skillLevel;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static void ValidateSkillLevel(int skillLevel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skillLevel);
    }
}
