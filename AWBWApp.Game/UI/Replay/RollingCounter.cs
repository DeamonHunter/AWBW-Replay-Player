using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;

namespace AWBWApp.Game.UI.Replay
{
    public partial class RollingCounter<T> : Container, IHasCurrentValue<T>
        where T : struct, IEquatable<T>
    {
        public Drawable DrawableCount { get; private set; }

        private string prefix;

        public string Prefix
        {
            get => prefix;
            set
            {
                if (prefix == value)
                    return;

                prefix = value;
                UpdateDisplay();
            }
        }

        private string suffix;

        public string Suffix
        {
            get => suffix;
            set
            {
                if (suffix == value)
                    return;

                suffix = value;
                UpdateDisplay();
            }
        }

        public T DisplayedCount
        {
            get => displayedCount;
            set
            {
                if (EqualityComparer<T>.Default.Equals(displayedCount, value))
                    return;
                displayedCount = value;
                UpdateDisplay();
            }
        }

        private T displayedCount;

        public Bindable<T> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        public FontUsage Font
        {
            get => displayedCountText.Font;
            set => displayedCountText.Font = value;
        }

        private readonly BindableWithCurrent<T> current = new BindableWithCurrent<T>();

        protected bool IsRollingProportionalToChange => false;
        public double RollingDuration = 500;
        protected virtual Easing RollingEasing => Easing.OutQuint;

        private SpriteText displayedCountText;

        public RollingCounter()
        {
            AutoSizeAxes = Axes.Both;
            displayedCountText = CreateText();
            Child = DrawableCount = displayedCountText;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            UpdateDisplay();
        }

        protected void UpdateDisplay()
        {
            if (displayedCountText != null)
                displayedCountText.Text = Prefix + FormatCount(DisplayedCount) + Suffix;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Current.BindValueChanged(val => TransformCount(DisplayedCount, val.NewValue), true);
        }

        protected void TransformCount(T currentValue, T newValue)
        {
            double rollingTotalDuration = RollingDuration;
            //IsRollingProportionalToChange ? GetProportionalDuration(currentValue, newValue) : RollingDuration;

            this.TransformTo(nameof(DisplayedCount), newValue, rollingTotalDuration, RollingEasing);
        }

        //protected double GetProportionalDuration(T currentValue, T newValue) => currentValue > newValue ? currentValue - newValue : newValue - currentValue;

        protected LocalisableString FormatCount(T count) => count.ToString();

        protected SpriteText CreateText() => new SpriteText();
    }
}
