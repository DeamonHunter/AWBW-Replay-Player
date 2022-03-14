using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Toolbar
{
    public class DrawableAWBWMenuItem : Menu.DrawableMenuItem
    {
        private TextContainer text;

        private Action<bool, Drawable> onHoverChange;

        public DrawableAWBWMenuItem(MenuItem item, Action<bool, Drawable> onHoverChange)
            : base(item)
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;

            this.onHoverChange = onHoverChange;

            BackgroundColour = new Color4(40, 40, 40, 255);
            BackgroundColourHover = new Color4(70, 70, 70, 255);
        }

        protected override bool OnHover(HoverEvent e)
        {
            onHoverChange.Invoke(true, this);
            updateState();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            onHoverChange.Invoke(false, this);
            updateState();
            base.OnHoverLost(e);
        }

        private void updateState()
        {
            Alpha = Item.Action.Disabled ? 0.2f : 1f;

            if (IsHovered && !Item.Action.Disabled)
            {
                text.BoldText.FadeIn(80, Easing.OutQuint);
                text.NormalText.FadeOut(80, Easing.OutQuint);
            }
            else
            {
                text.BoldText.FadeOut(80, Easing.OutQuint);
                text.NormalText.FadeIn(80, Easing.OutQuint);
            }
        }

        protected override Drawable CreateContent() => text = CreateTextContainer();

        protected virtual TextContainer CreateTextContainer() => new TextContainer();

        public class TextContainer : Container, IHasText
        {
            public LocalisableString Text
            {
                get => NormalText.Text;
                set
                {
                    NormalText.Text = value;
                    BoldText.Text = value;
                }
            }

            public readonly SpriteText NormalText;
            public readonly SpriteText BoldText;

            public TextContainer()
            {
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;

                AutoSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    NormalText = new SpriteText()
                    {
                        AlwaysPresent = true, //Ensure that the sizing doesn't change when going between bold/not bold
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Font = new FontUsage("Roboto", 16),
                        Margin = new MarginPadding { Horizontal = 10, Vertical = 5 },
                    },
                    BoldText = new SpriteText()
                    {
                        AlwaysPresent = true, //Ensure that the sizing doesn't change when going between bold/not bold
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Font = new FontUsage("Roboto", 16, "Bold"),
                        Alpha = 0,
                        Margin = new MarginPadding { Horizontal = 10, Vertical = 5 },
                    },
                };
            }
        }
    }
}
