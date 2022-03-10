using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace AWBWApp.Game.UI.Replay.Toolbar
{
    public class DrawableToggleMenuItem : DrawableAWBWMenuItem
    {
        protected new ToggleMenuItem Item => (ToggleMenuItem)base.Item;

        public DrawableToggleMenuItem(MenuItem item, Action<bool, Drawable> onHoverChange)
            : base(item, onHoverChange)
        {
        }

        protected override TextContainer CreateTextContainer() => new ToggleTextContainer(Item);

        private class ToggleTextContainer : TextContainer
        {
            private readonly ToggleMenuItem toggleItem;
            private readonly Bindable<bool> state;
            private readonly SpriteIcon stateIcon;

            public ToggleTextContainer(ToggleMenuItem menuItem)
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

            private void updateState(ValueChangedEvent<bool> state)
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
