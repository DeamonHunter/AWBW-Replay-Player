using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace AWBWApp.Game.UI.Weather
{
    public class SnowAnimation : RainAnimation
    {
        protected override Vector2 ParticleBaseSize => new Vector2(25, 25);
        protected override float ParticleBaseVelocity => 250;
        protected override Vector2 ParticleRandomAngle => new Vector2(0.1f, 0.3f);
        protected override float ParticleCountChangeSmoothing => 0.75f;

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            Texture = store.Get("Effects/Snow");
        }
    }
}
