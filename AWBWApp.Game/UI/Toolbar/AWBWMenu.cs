using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Toolbar
{
    public partial class AWBWMenu : Menu
    {
        private HashSet<Drawable> hoveredDrawables = new HashSet<Drawable>();

        public AWBWMenu()
            : base(Direction.Horizontal, true)
        {
            RelativeSizeAxes = Axes.X;
            BackgroundColour = new Color4(25, 25, 25, 255);
        }

        public bool IsActive => IsHovered || hoveredDrawables.Count > 0 || InternalChildren[1].Size != Vector2.Zero;

        protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableAWBWMenuItemWithHeader(item, onHoverChange);

        protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

        protected override Menu CreateSubMenu() => new AWBWSubMenu(this, onHoverChange);

        private void onHoverChange(bool gainedHover, Drawable drawable)
        {
            if (gainedHover)
                hoveredDrawables.Add(drawable);
            else
                hoveredDrawables.Remove(drawable);
        }

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case ScrollEvent _:
                    if (ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
                        return true;
                    break;

                case MouseEvent _:
                    return true;
            }
            return base.Handle(e);
        }
    }
}
