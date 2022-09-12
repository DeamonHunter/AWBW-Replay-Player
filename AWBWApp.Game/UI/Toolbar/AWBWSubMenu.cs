using System;
using System.Collections.Generic;
using AWBWApp.Game.UI.Components.Menu;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;

namespace AWBWApp.Game.UI.Toolbar
{
    public class AWBWSubMenu : Menu
    {
        private readonly Menu parentMenu;

        private readonly Action<bool, Drawable> baseOnHoverChange;
        private readonly HashSet<Drawable> hoveredDrawables = new HashSet<Drawable>();

        public bool HideSubMenuIfUnHovered = true;
        public bool HideIfUnHovered = true;

        public AWBWSubMenu(Menu parentMenu, Action<bool, Drawable> onHoverChange)
            : base(Direction.Vertical)
        {
            this.parentMenu = parentMenu;
            baseOnHoverChange = onHoverChange;
        }

        protected override bool OnHover(HoverEvent e)
        {
            baseOnHoverChange?.Invoke(true, this);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            baseOnHoverChange?.Invoke(false, this);
            base.OnHoverLost(e);
        }

        private void onHoverChange(bool gainedHover, Drawable drawable)
        {
            if (gainedHover)
                hoveredDrawables.Add(drawable);
            else
                hoveredDrawables.Remove(drawable);
            baseOnHoverChange?.Invoke(gainedHover, drawable);
        }

        protected override void Update()
        {
            base.Update();

            if (!HideIfUnHovered || State != MenuState.Open || parentMenu == null)
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

        protected override Menu CreateSubMenu() =>
            new AWBWSubMenu(this, onHoverChange)
            {
                HideIfUnHovered = HideSubMenuIfUnHovered
            };

        protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item)
        {
            switch (item)
            {
                case SliderMenuItem:
                    return new DrawableSliderMenuItem(item, onHoverChange);

                case StatefulMenuItem:
                    return new DrawableStatefulMenuItem(item, onHoverChange);

                case ToggleMenuItem:
                    return new DrawableToggleMenuItem(item, onHoverChange);

                case ColourPickerMenuItem:
                    return new DrawableColourPickerMenuItem(item, onHoverChange);
            }

            return new DrawableAWBWMenuItem(item, onHoverChange);
        }

        protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new BasicScrollContainer(direction);

        protected override void OnFocusLost(FocusLostEvent e)
        {
            //This is needed to make sure colour picker text boxes don't cause issues.
            var drawableToCheck = e.NextFocused;

            while (drawableToCheck?.Parent != null)
            {
                if (drawableToCheck.Parent == this)
                    return;

                drawableToCheck = drawableToCheck.Parent;
            }

            base.OnFocusLost(e);
        }
    }
}
