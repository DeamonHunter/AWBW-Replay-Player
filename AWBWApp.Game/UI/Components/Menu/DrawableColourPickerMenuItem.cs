using System;
using AWBWApp.Game.UI.Toolbar;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;

namespace AWBWApp.Game.UI.Components.Menu
{
    public partial class DrawableColourPickerMenuItem : DrawableAWBWMenuItem
    {
        protected new ColourPickerMenuItem Item => (ColourPickerMenuItem)base.Item;

        public DrawableColourPickerMenuItem(MenuItem item, Action<bool, Drawable> onHoverChange)
            : base(item, onHoverChange)
        {
            BackgroundColourHover = BackgroundColour;
        }

        protected override InnerMenuContainer CreateInnerMenuContainer() => new SliderInnerMenuContainer(Item);

        private partial class SliderInnerMenuContainer : InnerMenuContainer
        {
            private readonly ColourPickerMenuItem colourPickerItem;
            private readonly ColourPicker colourPicker;
            private readonly BasicButton button;

            public SliderInnerMenuContainer(ColourPickerMenuItem menuItem)
            {
                colourPickerItem = menuItem;

                var padding = NormalText.Margin;
                padding.Right += 20;

                NormalText.Margin = padding;
                BoldText.Margin = padding;

                Add(new FillFlowContainer()
                {
                    Direction = FillDirection.Vertical,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        colourPicker = new AWBWColourPicker(),
                        button = new BasicButton()
                        {
                            RelativeSizeAxes = Axes.X,
                            Padding = new MarginPadding { Horizontal = 5, Bottom = 5 },
                            Height = 35,
                            Text = "Reset to Default",
                            Action = () => colourPicker.Current.Value = colourPickerItem.State.Default
                        }
                    }
                });

                colourPicker.Current.BindTo(colourPickerItem.State);
                colourPicker.Current.BindValueChanged(x => button.Enabled.Value = x.NewValue != colourPickerItem.State.Default, true);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                colourPicker.Current.BindValueChanged(updateState, true);
            }

            protected override bool OnClick(ClickEvent e)
            {
                return true;
            }

            private void updateState(ValueChangedEvent<Colour4> state)
            {
                Text = state.NewValue.ToString();
            }
        }

        private partial class AWBWColourPicker : BasicColourPicker
        {
            protected override HSVColourPicker CreateHSVColourPicker() => new AWBWHSVColourPicker();
            protected override HexColourPicker CreateHexColourPicker() => new AWBWHexColourPicker();

            private partial class AWBWHSVColourPicker : BasicHSVColourPicker
            {
                public override bool AcceptsFocus => true;

                public AWBWHSVColourPicker()
                {
                    Background.Colour = new Colour4(40, 40, 40, 255);
                    Content.Padding = new MarginPadding { Horizontal = 8, Top = 8, Bottom = 4 };
                    Content.Spacing = new Vector2(0, 5);
                }

                protected override HueSelector CreateHueSelector() => new AWBWHueSelector();

                protected override SaturationValueSelector CreateSaturationValueSelector() => new AWBWSaturationValueSelector();

                protected override bool OnClick(ClickEvent e)
                {
                    return true;
                }

                private partial class AWBWHueSelector : BasicHSVColourPicker.BasicHueSelector
                {
                    public override bool AcceptsFocus => true;

                    protected override bool OnClick(ClickEvent e)
                    {
                        return true;
                    }
                }

                private partial class AWBWSaturationValueSelector : BasicHSVColourPicker.BasicSaturationValueSelector
                {
                    public override bool AcceptsFocus => true;

                    protected override bool OnClick(ClickEvent e)
                    {
                        return true;
                    }
                }
            }

            private partial class AWBWHexColourPicker : BasicHexColourPicker
            {
                public override bool AcceptsFocus => true;

                public AWBWHexColourPicker()
                {
                    Background.Colour = new Colour4(40, 40, 40, 255);
                    Padding = new MarginPadding { Horizontal = 8, Top = 4, Bottom = 8 };
                    Spacing = 5f;
                }

                protected override bool OnClick(ClickEvent e)
                {
                    return true;
                }
            }
        }
    }
}
