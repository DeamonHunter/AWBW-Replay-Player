using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor.Components
{
    public partial class TemporaryPopupMessage : CompositeDrawable
    {
        private SpriteText spriteText;

        private const float time_visible = 1500;

        public TemporaryPopupMessage(LocalisableString text)
        {
            AutoSizeAxes = Axes.Both;
            Masking = true;
            CornerRadius = 6;

            AddRangeInternal(new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(40, 40, 40, 180)
                },
                spriteText = new SpriteText()
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    MaxWidth = 500,
                    Text = text,
                    Margin = new MarginPadding(5)
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            this.ScaleTo(new Vector2(0, 0.8f)).ScaleTo(1, 200, Easing.OutQuint)
                .FadeOut().FadeIn(225, Easing.OutQuint)
                .Then(time_visible)
                .ScaleTo(new Vector2(0, 0.8f), 200, Easing.InQuint)
                .FadeOut(200, Easing.InQuint).Expire();
        }
    }
}
