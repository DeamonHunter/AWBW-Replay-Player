using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.UI.Weather;
using NUnit.Framework;
using osu.Framework.Testing;

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public class TestSceneWeather : AWBWAppTestScene
    {
        private WeatherAnimationController weatherController;

        public TestSceneWeather()
        {
            Child = weatherController = new WeatherAnimationController();
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Reset", () =>
            {
                weatherController.CurrentWeather.Value = WeatherType.Clear;
                weatherController.ParticleMultiplier = 1;
            });
        }

        [Test]
        public void TestWeather()
        {
            AddStep("Display Clear", () => weatherController.CurrentWeather.Value = WeatherType.Clear);
            AddStep("Display Rain", () => weatherController.CurrentWeather.Value = WeatherType.Rain);
            AddStep("Display Snow", () => weatherController.CurrentWeather.Value = WeatherType.Snow);
        }

        [TestCase(WeatherType.Rain)]
        [TestCase(WeatherType.Snow)]
        public void TestPowerChangeWeather(WeatherType weather)
        {
            AddStep("Display Weather with increased particles", () =>
            {
                weatherController.CurrentWeather.Value = weather;
                weatherController.ParticleMultiplier = 3;
                weatherController.ParticleVelocity = 1.25f;
            });
            AddStep("Reset to normal", () =>
            {
                weatherController.CurrentWeather.Value = weather;
                weatherController.ParticleMultiplier = 1;
                weatherController.ParticleVelocity = 1;
            });
        }
    }
}
