using System;
using System.Collections.Generic;
using AWBWApp.Game.UI.Replay.Toolbar;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Toolbar
{
    public class AWBWMenu : Menu
    {
        private HashSet<Drawable> hoveredDrawables = new HashSet<Drawable>();

        public AWBWMenu()
            : base(Direction.Horizontal, true)
        {
            RelativeSizeAxes = Axes.X;
            Size = new Vector2(1, 0);
            BackgroundColour = new Color4(25, 25, 25, 255);
        }

        public bool IsActive => IsHovered || hoveredDrawables.Count > 0 || InternalChildren[1].Size != Vector2.Zero;

        protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableAWBWMenuItemWithHeader(item, onHoverChange);

        protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

        protected override Menu CreateSubMenu() => new SubMenu(this, onHoverChange);

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

        private class SubMenu : Menu
        {
            private readonly Menu parentMenu;

            private readonly Action<bool, Drawable> baseOnHoverChange;
            private readonly HashSet<Drawable> hoveredDrawables = new HashSet<Drawable>();

            public SubMenu(Menu parentMenu, Action<bool, Drawable> onHoverChange)
                : base(Direction.Vertical)
            {
                this.parentMenu = parentMenu;
                baseOnHoverChange = onHoverChange;
            }

            protected override bool OnHover(HoverEvent e)
            {
                baseOnHoverChange.Invoke(true, this);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                baseOnHoverChange.Invoke(false, this);
                base.OnHoverLost(e);
            }

            private void onHoverChange(bool gainedHover, Drawable drawable)
            {
                if (gainedHover)
                    hoveredDrawables.Add(drawable);
                else
                    hoveredDrawables.Remove(drawable);
                baseOnHoverChange.Invoke(gainedHover, drawable);
            }

            protected override void Update()
            {
                base.Update();

                if (State != MenuState.Open)
                    return;

                if (parentMenu.IsHovered || IsHovered || hoveredDrawables.Count > 0 || InternalChildren[1].Size != Vector2.Zero)
                    return;

                Close();
            }

            protected override void UpdateSize(Vector2 newSize)
            {
                Width = newSize.X;
                this.ResizeHeightTo(newSize.Y, 300, Easing.OutQuint);
            }

            protected override void AnimateOpen()
            {
                this.FadeIn(300, Easing.OutQuint);
            }

            protected override void AnimateClose()
            {
                this.FadeOut(300, Easing.OutQuint);
            }

            protected override Menu CreateSubMenu() => new SubMenu(this, onHoverChange);

            protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item)
            {
                switch (item)
                {
                    case ToggleMenuItem toggle:
                        return new DrawableToggleMenuItem(toggle, onHoverChange);
                }

                return new DrawableAWBWMenuItem(item, onHoverChange);
            }

            protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);
        }
    }
}
