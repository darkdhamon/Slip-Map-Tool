using Newtonsoft.Json;

namespace SlipMap.Model.MapElements;

public struct RadialMapCoordinates
{
    /// <summary>
    /// Angular offset from Galactic North 0 Degrees.
    /// </summary>
    public double AngleAsDegrees { get; set; }
    /// <summary>
    /// Distance LY from the center of the galaxy
    /// </summary>
    public double Radius { get; set; }
    /// <summary>
    /// Distance away from the galactic plane
    /// </summary>
    public double Offset { get; set; }

    /// <summary>
    /// Angular offset from galactic north 0 Radians
    /// </summary>
    private double AngleAsRadians => (Math.PI / 180) * AngleAsDegrees;
    /// <summary>
    /// 3D X Coordinate
    /// </summary>
    public double X => Math.Round(Radius*Math.Cos(AngleAsRadians),3);
    /// <summary>
    /// 3D Y Coordinate
    /// </summary>
    public double Y => Math.Round(Radius*Math.Sin(AngleAsRadians),3);
    /// <summary>
    /// 3D Z Coordinate
    /// </summary>
    public double Z => Offset;

    public override string ToString()
    {
        return
            $"Galactic Coordinates: ARO({AngleAsDegrees} Deg, {Radius} LY, {Offset} LY) XYZ({X} Ly, {Y} LY, {Z} LY)";
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }

    public void FromJson(string value)
    {
        var deserializedCoordinates = JsonConvert.DeserializeObject<RadialMapCoordinates>(value);
        AngleAsDegrees = deserializedCoordinates.AngleAsDegrees;
        Radius = deserializedCoordinates.Radius;
        Offset = deserializedCoordinates.Offset;
    }
}