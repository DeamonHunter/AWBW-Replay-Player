using System;
using AWBWApp.Game.UI.Toolbar;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace AWBWApp.Game.UI.Components.Menu
{
    public class DrawableSliderMenuItem : DrawableAWBWMenuItem
    {
        protected new SliderMenuItem Item => (SliderMenuItem)base.Item;

        public DrawableSliderMenuItem(MenuItem item, Action<bool, Drawable> onHoverChange)
            : base(item, onHoverChange)
        {
        }

        protected override InnerMenuContainer CreateInnerMenuContainer() => new SliderInnerMenuContainer(Item);

        private class SliderInnerMenuContainer : InnerMenuContainer
        {
            private readonly SliderMenuItem sliderItem;
            private readonly KnobSliderBar<float> sliderBar;

            public SliderInnerMenuContainer(SliderMenuItem menuItem)
            {
                sliderItem = menuItem;

                var padding = NormalText.Margin;
                padding.Right += 20;

                NormalText.Margin = padding;
                BoldText.Margin = padding;

                Add(sliderBar = new KnobSliderBar<float>
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Size = new Vector2(150, 20),
                    Margin = new MarginPadding { Right = 4, Left = 40 },
                    AlwaysPresent = true
                });
                sliderBar.Current.BindTo(sliderItem.State);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                sliderBar.Current.BindValueChanged(updateState, true);
            }

            private void updateState(ValueChangedEvent<float> state)
            {
                Text = state.NewValue.ToString("F3");
            }
        }
    }
}
