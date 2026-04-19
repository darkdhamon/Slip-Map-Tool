using SlipMap.Domain.Services.Abstract;

namespace SlipMap.Domain.Services;

public sealed class SystemRandomNumberGenerator : IRandomNumberGenerator
{
    public int Next(int minValue, int maxValue)
    {
        return Random.Shared.Next(minValue, maxValue);
    }
}
