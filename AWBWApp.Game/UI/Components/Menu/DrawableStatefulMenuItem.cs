using System;
using AWBWApp.Game.UI.Toolbar;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace AWBWApp.Game.UI.Components.Menu
{
    public class DrawableStatefulMenuItem : DrawableAWBWMenuItem
    {
        protected new StatefulMenuItem Item => (StatefulMenuItem)base.Item;

        public DrawableStatefulMenuItem(MenuItem item, Action<bool, Drawable> onHoverChange)
            : base(item, onHoverChange)
        {
        }

        protected override InnerMenuContainer CreateInnerMenuContainer() => new ToggleInnerMenuContainer(Item);

        private class ToggleInnerMenuContainer : InnerMenuContainer
        {
            private readonly StatefulMenuItem toggleItem;
            private readonly Bindable<object> state;
            private readonly SpriteIcon stateIcon;

            public ToggleInnerMenuContainer(StatefulMenuItem menuItem)
            {
                toggleItem = menuItem;
                state = menuItem.State.GetBoundCopy();

                var padding = NormalText.Margin;
                padding.Right += 20;

                NormalText.Margin = padding;
                BoldText.Margin = padding;

                Add(stateIcon = new SpriteIcon
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Size = new Vector2(10),
                    Margin = new MarginPadding { Horizontal = 10 },
                    AlwaysPresent = true
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                state.BindValueChanged(updateState, true);
            }

            private void updateState(ValueChangedEvent<object> state)
            {
                var icon = toggleItem.GetIconForState(state.NewValue);

                if (icon == null)
                    stateIcon.Alpha = 0;
                else
                {
                    stateIcon.Alpha = 1;
                    stateIcon.Icon = icon.Value;
                }
            }
        }
    }
}
