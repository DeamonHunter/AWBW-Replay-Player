using System;
using System.Collections.Generic;
using AWBWApp.Game.UI.Replay.Toolbar;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class ReplayMenuBar : Menu
    {
        public Bindable<MenuState> MenuShown = new Bindable<MenuState>();
        public bool KeepOpen = false;

        public float HideDelay = 1000;

        private HashSet<Drawable> hoveredDrawables = new HashSet<Drawable>();
        private ScheduledDelegate hoverHideDelegate;

        public ReplayMenuBar()
            : base(Direction.Horizontal, true)
        {
            RelativeSizeAxes = Axes.X;
            Size = new Vector2(1, 40);
            MenuShown.BindValueChanged(x => updateMenuShown(x.NewValue));

            BackgroundColour = new Color4(25, 25, 25, 255);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AnimateClose();
            FinishTransforms(true);
        }

        protected override void Update()
        {
            base.Update();

            if (MenuShown.Value != MenuState.Open)
                return;

            if (IsHovered || InternalChildren[1].Size != Vector2.Zero || hoveredDrawables.Count > 0 || KeepOpen)
            {
                if (hoverHideDelegate != null)
                {
                    hoverHideDelegate.Cancel();
                    hoverHideDelegate = null;
                }
                return;
            }

            if (hoverHideDelegate == null)
                hoverHideDelegate = Scheduler.AddDelayed(() => MenuShown.Value = MenuState.Closed, HideDelay);
        }

        private void updateMenuShown(MenuState newValue)
        {
            if (newValue == MenuState.Open)
                AnimateOpen();
            else
                AnimateClose();
        }

        protected override void AnimateOpen()
        {
            MenuShown.Value = MenuState.Open;
            this.FadeIn(300, Easing.OutQuint);
            this.ScaleTo(Vector2.One, 300, Easing.OutQuint);
        }

        protected override void AnimateClose()
        {
            MenuShown.Value = MenuState.Closed;
            this.FadeOut(300, Easing.OutQuint);
            this.ScaleTo(new Vector2(1, 0), 300, Easing.OutQuint);
        }

        private void onHoverChange(bool gainedHover, Drawable drawable)
        {
            if (gainedHover)
                hoveredDrawables.Add(drawable);
            else
                hoveredDrawables.Remove(drawable);
        }

        protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableReplayMenuItemWithHeader(item, onHoverChange);

        protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

        protected override Menu CreateSubMenu() => new SubMenu(this, onHoverChange);

        private class SubMenu : Menu
        {
            private Menu parentMenu;

            private Action<bool, Drawable> baseOnHoverChange;
            private HashSet<Drawable> hoveredDrawables = new HashSet<Drawable>();

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

                return new DrawableReplayMenuItem(item, onHoverChange);
            }

            protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);
        }
    }
}
