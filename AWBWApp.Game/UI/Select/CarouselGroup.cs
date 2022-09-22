using System;
using System.Collections.Generic;

namespace AWBWApp.Game.UI.Select
{
    public class CarouselGroup : CarouselItem
    {
        public override DrawableCarouselItem GetDrawableForItem() => null;

        public IReadOnlyList<CarouselItem> Children => InternalChildren;

        protected List<CarouselItem> InternalChildren = new List<CarouselItem>();

        private ulong currentChildId;

        public CarouselGroup(List<CarouselItem> items = null)
        {
            if (items != null) InternalChildren = items;

            State.ValueChanged += state =>
            {
                switch (state.NewValue)
                {
                    case CarouselItemState.Collapsed:
                    case CarouselItemState.NotSelected:
                        InternalChildren.ForEach(c => c.State.Value = CarouselItemState.Collapsed);
                        break;

                    case CarouselItemState.Selected:
                        InternalChildren.ForEach(c =>
                        {
                            if (c.State.Value == CarouselItemState.Collapsed) c.State.Value = CarouselItemState.NotSelected;
                        });
                        break;
                }
            };
        }

        protected virtual void ChildItemStateChanged(CarouselItem item, CarouselItemState value)
        {
            if (value == CarouselItemState.Selected)
            {
                foreach (var c in InternalChildren)
                {
                    if (item == c) continue;
                    c.State.Value = CarouselItemState.NotSelected;
                }
                State.Value = CarouselItemState.Selected;
            }
        }

        public virtual void AddChild(CarouselItem i)
        {
            i.State.ValueChanged += state => ChildItemStateChanged(i, state.NewValue);
            i.ChildID = ++currentChildId;
            InternalChildren.Add(i);
        }

        public virtual void RemoveChild(CarouselItem i)
        {
            InternalChildren.Remove(i);

            // it's important we do the deselection after removing, so any further actions based on
            // State.ValueChanged make decisions post-removal.
            i.State.Value = CarouselItemState.Collapsed;
        }

        public override void Filter(string[] textParts, CarouselFilter filter)
        {
            foreach (var child in InternalChildren)
                child.Filter(textParts, filter);
        }

        public virtual void Sort(Comparison<CarouselItem> comparison)
        {
            InternalChildren.Sort(comparison);
        }

        public override void UnbindBindables()
        {
            foreach (var child in InternalChildren)
                child.UnbindBindables();

            base.UnbindBindables();
        }
    }
}
