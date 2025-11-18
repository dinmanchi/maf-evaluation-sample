namespace MafEvaluationSample;

public class WeatherTool
{
    private static readonly Random _random = new();
    private static readonly string[] _conditions = { "sunny", "cloudy", "rainy", "stormy" };

    public static string GetWeather(string location)
    {
        var condition = _conditions[_random.Next(_conditions.Length)];
        var temperature = _random.Next(10, 31);
        return $"The weather in {location} is {condition} with a high of {temperature}°C.";
    }
}
