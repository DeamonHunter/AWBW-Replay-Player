using System;
using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
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
    public class PowerProgress : Container, IHasCurrentValue<int>, IHasTooltip
    {
        private readonly BindableWithCurrent<int> current = new BindableWithCurrent<int>();

        public Bindable<int> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private int displayedPower;

        public int DisplayedValue
        {
            get => displayedPower;
            set
            {
                if (displayedPower == value)
                    return;
                displayedPower = value;
                UpdatePower();
            }
        }

        private List<PowerSegment> segments = new List<PowerSegment>();

        public int PowerRequiredForSuper => segments.Count * ProgressPerBar;
        public int PowerRequiredForNormal => smallBars * ProgressPerBar;

        public int ProgressPerBar = 90000;

        private readonly int smallBars;

        public override bool HandlePositionalInput => true;

        public PowerProgress(int? requiredNormal, int? requiredSuper)
        {
            if (!requiredNormal.HasValue && !requiredSuper.HasValue)
                throw new Exception("RequiredPowers cannot both be null.");

            //AWBW actually makes both powers the same.
            if (requiredNormal == requiredSuper)
                requiredNormal = null;

            smallBars = requiredNormal.HasValue ? requiredNormal.Value / ProgressPerBar : 0;
            var largeBars = requiredSuper.HasValue ? (requiredSuper.Value / ProgressPerBar) - smallBars : 0;
            var barWidth = 1f / (smallBars + largeBars);

            for (int i = 0; i < smallBars; i++)
            {
                var child = new PowerSegment(false)
                {
                    RelativePositionAxes = Axes.X,
                    Position = new Vector2(barWidth * segments.Count, 0),
                    Width = barWidth
                };

                Add(child);
                segments.Add(child);
            }

            for (int i = 0; i < largeBars; i++)
            {
                var child = new PowerSegment(true)
                {
                    RelativePositionAxes = Axes.X,
                    Position = new Vector2(barWidth * segments.Count, 0),
                    Width = barWidth
                };

                Add(child);
                segments.Add(child);
            }

            Current.BindValueChanged(val => TransformPower(DisplayedValue, val.NewValue));
        }

        public LocalisableString TooltipText => $"Normal: {Math.Floor(Math.Min(displayedPower, PowerRequiredForNormal) / 10.0f)} / {Math.Floor(PowerRequiredForNormal / 10.0f)}\nSuper:    {Math.Floor(Math.Min(displayedPower, PowerRequiredForSuper) / 10.0f)} / {Math.Floor(PowerRequiredForSuper / 10.0f)}";

        public void UpdatePower()
        {
            var power = displayedPower;

            var hasPower = power >= ProgressPerBar * smallBars;
            var hasSuperPower = power >= ProgressPerBar * segments.Count;

            foreach (var segment in segments)
            {
                var countForSegment = Math.Max(0, Math.Min(ProgressPerBar, power));

                power -= countForSegment;
                segment.SegmentProgress = (float)countForSegment / ProgressPerBar;

                segment.Pulsating = segment.Super ? hasSuperPower : hasPower;
            }
        }

        public void TransformPower(int currentValue, int newValue)
        {
            this.TransformTo(nameof(DisplayedValue), newValue, 400, Easing.OutCubic);
        }

        private class PowerSegment : Container
        {
            private float progress;

            public float SegmentProgress
            {
                get => progress;
                set
                {
                    progress = value;
                    UpdateDisplay();
                }
            }

            private bool pulsating;

            public bool Pulsating
            {
                get => pulsating;
                set
                {
                    pulsating = value;
                    UpdatePulsating();
                }
            }

            public bool Super { get; private set; }

            private readonly Box fill;
            private readonly Color4 notFilledColor = Color4Extensions.FromHex("059113");
            private readonly Color4 filledColor = Color4Extensions.FromHex("0eaf1e").Lighten(0.25f);
            private readonly Color4 filledPulsateColor = Color4Extensions.FromHex("17d129").LightenAndFade(0.6f);

            public PowerSegment(bool super)
            {
                RelativeSizeAxes = Axes.Both;

                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;

                Super = super;

                Height = super ? 1 : 0.7f;

                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(0.4f)
                    },
                    fill = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = notFilledColor
                    },
                    new Container()
                    {
                        Masking = true,
                        RelativeSizeAxes = Axes.Both,
                        BorderColour = new Color4(200, 200, 200, 255),
                        BorderThickness = 3,
                        Child = new Box()
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0.1f,
                            AlwaysPresent = true
                        }
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                UpdateDisplay();
            }

            protected void UpdateDisplay()
            {
                fill.Width = progress;

                if (progress < 1)
                    pulsating = false;

                FinishTransforms();
                fill.FadeColour(progress >= 1 ? filledColor : notFilledColor, 200, Easing.OutQuint);
            }

            protected void UpdatePulsating()
            {
                if (!pulsating)
                {
                    UpdateDisplay();
                    return;
                }

                fill.FadeColour(filledColor, 200, Easing.OutQuint);
                fill.Delay(200).Loop(600, p => p.FadeColour(filledPulsateColor, 400, Easing.In).Then().FadeColour(filledColor, 600, Easing.In));
            }
        }
    }
}
