using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    public class StandardCloseButton : ClickableContainer
    {
        private Color4 buttonColour = new Color4(20, 20, 20, 255);

        public Color4 ButtonColour
        {
            get => buttonColour;
            set
            {
                buttonColour = value;
                if (!IsHovered)
                    Colour = value;
            }
        }

        public StandardCloseButton()
        {
            Colour = ButtonColour;
            AutoSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                new SpriteIcon()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Icon = FontAwesome.Solid.TimesCircle,
                    Size = new Vector2(20)
                }
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            this.FadeColour(new Color4(239, 155, 20, 255), 200);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            this.FadeColour(ButtonColour, 200);
            base.OnHoverLost(e);
        }
    }
}
