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
        private InnerMenuContainer innerMenu;

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
            UpdateHover();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            onHoverChange.Invoke(false, this);
            UpdateHover();
            base.OnHoverLost(e);
        }

        protected virtual void UpdateHover()
        {
            Alpha = Item.Action.Disabled ? 0.2f : 1f;

            if (IsHovered && !Item.Action.Disabled)
            {
                innerMenu.BoldText.FadeIn(80, Easing.OutQuint);
                innerMenu.NormalText.FadeOut(80, Easing.OutQuint);
            }
            else
            {
                innerMenu.BoldText.FadeOut(80, Easing.OutQuint);
                innerMenu.NormalText.FadeIn(80, Easing.OutQuint);
            }
        }

        protected override Drawable CreateContent() => innerMenu = CreateInnerMenuContainer();

        protected virtual InnerMenuContainer CreateInnerMenuContainer() => new InnerMenuContainer();

        public class InnerMenuContainer : Container, IHasText
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

            public InnerMenuContainer()
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
