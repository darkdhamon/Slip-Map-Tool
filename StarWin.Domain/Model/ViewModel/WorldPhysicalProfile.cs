namespace StarWin.Domain.Model.ViewModel;

public sealed class WorldPhysicalProfile
{
    public double GravityEarthG { get; set; }

    public double MassEarthMasses { get; set; }

    public double EscapeVelocityKmPerSecond { get; set; }

    public double OxygenPressureAtmospheres { get; set; }

    public double Albedo { get; set; }

    public int OrbitPeriodDays { get; set; }

    public int EccentricityTemperatureEffectCelsius { get; set; }

    public int SummerTemperatureIncreaseCelsius { get; set; }

    public int WinterTemperatureDecreaseCelsius { get; set; }

    public int MaximumDayTemperatureIncreaseCelsius { get; set; }

    public int MaximumNightTemperatureDecreaseCelsius { get; set; }

    public int EquatorTemperatureIncreaseCelsius { get; set; }

    public int PolarTemperatureDecreaseCelsius { get; set; }

    public int BoilingPointCelsius { get; set; }

    public double MagneticFieldGauss { get; set; }

    public IList<string> UnusualCharacteristics { get; } = new List<string>();
}
