using AWBWApp.Game.Game.Logic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace AWBWApp.Game.UI.Weather
{
    public partial class WeatherAnimationController : Container
    {
        public Bindable<WeatherType> CurrentWeather = new Bindable<WeatherType>();

        private float particleMultiplier = 1;

        public float ParticleMultiplier
        {
            get => particleMultiplier;
            set
            {
                particleMultiplier = value;
                updateWeather(CurrentWeather.Value, particleMultiplier, particleVelocity);
            }
        }

        private float particleVelocity = 1;

        public float ParticleVelocity
        {
            get => particleVelocity;
            set
            {
                particleVelocity = value;
                updateWeather(CurrentWeather.Value, particleMultiplier, particleVelocity);
            }
        }

        private RainAnimation rain;
        private SnowAnimation snow;
        private IBindable<bool> showWeather;

        public WeatherAnimationController()
        {
            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                snow = new SnowAnimation(),
                rain = new RainAnimation(),
            };
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager config)
        {
            showWeather = config.GetBindable<bool>(AWBWSetting.ReplayShowWeather);
            showWeather.BindValueChanged(x => this.FadeTo(x.NewValue ? 1 : 0, 250, Easing.OutQuint), true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            CurrentWeather.BindValueChanged(x => updateWeather(x.NewValue, particleMultiplier, particleVelocity), true);
        }

        private void updateWeather(WeatherType weather, float particleMultiplier, float particleVelocity)
        {
            switch (weather)
            {
                case WeatherType.Clear:
                    rain.ParticleSpawnMultiplier = 0;
                    snow.ParticleSpawnMultiplier = 0;
                    break;

                case WeatherType.Rain:
                    rain.ParticleSpawnMultiplier = particleMultiplier;
                    rain.Velocity = particleVelocity;
                    snow.ParticleSpawnMultiplier = 0;
                    break;

                case WeatherType.Snow:
                    rain.ParticleSpawnMultiplier = 0;
                    snow.ParticleSpawnMultiplier = particleMultiplier;
                    snow.Velocity = particleVelocity;
                    break;
            }
        }
    }
}
