using System;

namespace AWBWApp.Game.Game.Logic
{
    public enum Weather
    {
        Clear,
        Rain,
        Snow
    }

    public static class WeatherHelper
    {
        public static Weather ParseWeatherCode(string code)
        {
            switch (code)
            {
                case "R":
                case "r":
                    return Weather.Rain;

                case "S":
                case "s":
                    return Weather.Snow;

                case "C":
                case "c":
                    return Weather.Clear;

                default:
                    throw new Exception("Unknown weather code: " + code);
            }
        }
    }
}
