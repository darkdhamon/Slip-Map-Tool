namespace SlipMap.Domain.Services.Abstract;

public interface IRandomNumberGenerator
{
    int Next(int minValue, int maxValue);
}
