using System;

namespace AWBWApp.Game.Game.Logic
{
    public enum WeatherType
    {
        Clear,
        Rain,
        Snow
    }

    public static class WeatherHelper
    {
        public static WeatherType ParseWeatherCode(string code)
        {
            switch (code)
            {
                case "R":
                case "r":
                    return WeatherType.Rain;

                case "S":
                case "s":
                    return WeatherType.Snow;

                case "C":
                case "c":
                    return WeatherType.Clear;

                default:
                    throw new Exception("Unknown weather code: " + code);
            }
        }
    }
}
