using System;
using osu.Framework.Bindables;

namespace AWBWApp.Game.UI.Select
{
    public abstract class CarouselItem : IComparable<CarouselItem>
    {
        public virtual float TotalHeight => 0;

        /// <summary>
        /// An externally defined value used to determine this item's vertical display offset relative to the carousel.
        /// </summary>
        public float CarouselYPosition;

        public readonly BindableBool Filtered = new BindableBool();

        public readonly Bindable<CarouselItemState> State = new Bindable<CarouselItemState>();

        public bool Visible => State.Value != CarouselItemState.Collapsed && !Filtered.Value;

        internal ulong ChildID;

        public CarouselItem()
        {
            Filtered.ValueChanged += filtered =>
            {
                if (filtered.NewValue && State.Value == CarouselItemState.Selected)
                    State.Value = CarouselItemState.NotSelected;
            };
        }

        public abstract DrawableCarouselItem GetDrawableForItem();

        public abstract void Filter(string[] textParts, CarouselFilter filter);

        public int CompareTo(CarouselItem other) => CarouselYPosition.CompareTo(other.CarouselYPosition);

        public virtual void UnbindBindables()
        {
            State.UnbindAll();
        }
    }

    public enum CarouselItemState
    {
        Collapsed,
        NotSelected,
        Selected
    }
}
