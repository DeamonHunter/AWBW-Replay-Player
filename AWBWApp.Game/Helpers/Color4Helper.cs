using osuTK.Graphics;

namespace AWBWApp.Game.Helpers
{
    public static class Color4Helper
    {
        public static Color4 LightenAndFade(this Color4 color, float amount)
        {
            var inverseAmount = 1 - amount;

            return new Color4(amount + inverseAmount * color.R, amount + inverseAmount * color.G, amount + inverseAmount * color.B, color.A);
        }
    }
}
