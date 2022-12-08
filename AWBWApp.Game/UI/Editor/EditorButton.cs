using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorButton : BasicButton
    {
        public EditorButton()
        {
            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;

            Masking = true;
            CornerRadius = 6;
            RelativeSizeAxes = Axes.X;
            Width = 0.9f;
            Height = 30;

            BackgroundColour = new Color4(20, 20, 20, 150);
            SpriteText.Colour = Color4.White;
        }
    }
}
