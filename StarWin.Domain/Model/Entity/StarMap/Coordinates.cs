namespace StarWin.Domain.Model.Entity.StarMap;

public readonly record struct Coordinates(short X, short Y, short Z)
{
    public double XParsecs => X / 10d;

    public double YParsecs => Y / 10d;

    public double ZParsecs => Z / 10d;

    public override string ToString()
    {
        return $"{XParsecs:0.#}, {YParsecs:0.#}, {ZParsecs:0.#}";
    }
}
