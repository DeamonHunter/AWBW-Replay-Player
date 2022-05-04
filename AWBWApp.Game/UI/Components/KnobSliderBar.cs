using System;
using System.Globalization;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    public class KnobSliderBar<T> : SliderBar<T>, IHasTooltip where T : struct, IComparable<T>, IConvertible, IEquatable<T>
    {
        private Color4 accentColour;

        public Color4 AccentColour
        {
            get => accentColour;
            set
            {
                accentColour = value;
                LeftBackground.Colour = value;
                Knob.Colour = value;
            }
        }

        private Color4 backgroundColour;

        public Color4 BackgroundColour
        {
            get => backgroundColour;
            set
            {
                backgroundColour = value;
                RightBackground.Colour = value;
            }
        }

        private string suffix;

        public string Suffix
        {
            get => suffix;
            set
            {
                if (value == suffix)
                    return;

                suffix = value;
                TooltipText = getTooltipText(CurrentNumber.Value);
            }
        }

        public bool DisplayAsPercentage { get; set; }

        protected readonly Circle Knob;
        private const int max_decimal_digits = 5;

        protected readonly Box LeftBackground;
        protected readonly Box RightBackground;

        public virtual LocalisableString TooltipText { get; private set; }

        public KnobSliderBar()
        {
            RangePadding = 15f;
            Padding = new MarginPadding { Horizontal = RangePadding };
            Children = new Drawable[]
            {
                new CircularContainer()
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    CornerRadius = 5,
                    Children = new Drawable[]
                    {
                        LeftBackground = new Box()
                        {
                            Height = 8,
                            EdgeSmoothness = new Vector2(0, 0.5f),
                            RelativeSizeAxes = Axes.None,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft
                        },
                        RightBackground = new Box()
                        {
                            Height = 8,
                            EdgeSmoothness = new Vector2(0, 0.5f),
                            RelativeSizeAxes = Axes.None,
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight
                        }
                    }
                },

                Knob = new Circle
                {
                    RelativePositionAxes = Axes.X,
                    Size = new Vector2(15),
                    Colour = Color4.White,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.CentreLeft
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            CurrentNumber.BindValueChanged(current => TooltipText = getTooltipText(current.NewValue), true);
        }

        private LocalisableString getTooltipText(T value)
        {
            if (CurrentNumber.IsInteger)
                return value.ToInt32(NumberFormatInfo.InvariantInfo).ToString("N0") + suffix;

            double floatValue = value.ToDouble(NumberFormatInfo.InvariantInfo);

            if (DisplayAsPercentage)
                return floatValue.ToString("0%") + suffix;

            decimal decimalPrecision = normalise(CurrentNumber.Precision.ToDecimal(NumberFormatInfo.InvariantInfo), max_decimal_digits);

            // Find the number of significant digits (we could have less than 5 after normalize())
            int significantDigits = findPrecision(decimalPrecision);

            return floatValue.ToString($"N{significantDigits}") + suffix;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            LeftBackground.Scale = new Vector2(Math.Clamp(RangePadding + Padding.Left + Knob.DrawPosition.X - (Knob.DrawWidth / DrawWidth) / 2, 0, DrawWidth), 1);
            RightBackground.Scale = new Vector2(Math.Clamp(DrawWidth - Knob.DrawPosition.X - RangePadding - Padding.Right - (Knob.DrawWidth / DrawWidth) / 2, 0, DrawWidth), 1);
        }

        protected override void UpdateValue(float value)
        {
            Knob.MoveToX(value, 250, Easing.OutQuint);
        }

        private int findPrecision(decimal d)
        {
            int precision = 0;

            while (d != Math.Round(d))
            {
                d *= 10;
                precision++;
            }

            return precision;
        }

        private decimal normalise(decimal d, int sd) => decimal.Parse(Math.Round(d, sd).ToString(string.Concat("0.", new string('#', sd)), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}
