using System;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace AWBWApp.Game.UI.Toolbar
{
    public partial class DrawableAWBWMenuItemWithHeader : DrawableAWBWMenuItem
    {
        private ExpandingBox backgroundBox;

        public DrawableAWBWMenuItemWithHeader(MenuItem item, Action<bool, Drawable> onHoverChange)
            : base(item, onHoverChange)
        {
            StateChanged += stateChanged;
        }

        protected override void UpdateBackgroundColour()
        {
            if (State == MenuItemState.Selected)
                backgroundBox?.FadeColour(BackgroundColourHover.Darken(0.3f));

            base.UpdateBackgroundColour();
        }

        protected override void UpdateForegroundColour()
        {
            if (State == MenuItemState.Selected)
                Foreground.FadeColour(ForegroundColourHover);
            else
                base.UpdateForegroundColour();
        }

        private void stateChanged(MenuItemState newState)
        {
            if (newState == MenuItemState.Selected)
                backgroundBox?.Expand();
            else
                backgroundBox?.Contract();
        }

        protected override Drawable CreateBackground()
        {
            var container = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    base.CreateBackground(),
                    backgroundBox = new ExpandingBox()
                }
            };

            return container;
        }

        private partial class ExpandingBox : CompositeDrawable
        {
            private readonly Container innerBackground;

            public ExpandingBox()
            {
                RelativeSizeAxes = Axes.Both;
                Masking = true;
                InternalChild = innerBackground = new Container()
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativeSizeAxes = Axes.X,
                    Masking = true,
                    CornerRadius = 4,
                    Position = new Vector2(0, -2),
                    Child = new Box { RelativeSizeAxes = Axes.Both }
                };
            }

            public void Expand() => innerBackground.Height = 3;
            public void Contract() => innerBackground.Height = 0;
        }
    }
}
