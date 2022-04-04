using System;
using System.Collections.Generic;
using System.Linq;

namespace AWBWApp.Game.UI.Select
{
    public class EagerSelectCarouselGroup : CarouselGroup
    {
        protected CarouselItem LastSelected { get; private set; }

        private int lastSelectedIndex;

        public void AddChildren(IEnumerable<CarouselItem> items)
        {
            foreach (var i in items)
                base.AddChild(i);
            attemptSelection();
        }

        public override void AddChild(CarouselItem i)
        {
            base.AddChild(i);
            attemptSelection();
        }

        public override void RemoveChild(CarouselItem i)
        {
            base.RemoveChild(i);

            if (i != LastSelected)
                updateSelectedIndex();
        }

        protected override void ChildItemStateChanged(CarouselItem item, CarouselItemState value)
        {
            base.ChildItemStateChanged(item, value);

            switch (value)
            {
                case CarouselItemState.Selected:
                    updateSelected(item);
                    break;

                case CarouselItemState.NotSelected:
                case CarouselItemState.Collapsed:
                    attemptSelection();
                    break;
            }
        }

        private void attemptSelection()
        {
            if (filteringChildren)
                return;

            if (State.Value != CarouselItemState.Selected)
                return;

            if (Children.Any(i => i.State.Value == CarouselItemState.Selected))
                return;

            PerformSelection();
        }

        protected virtual CarouselItem GetNextToSelect()
        {
            return Children.Skip(lastSelectedIndex).FirstOrDefault(i => !i.Filtered.Value) ??
                   Children.Reverse().Skip(InternalChildren.Count - lastSelectedIndex).FirstOrDefault(i => !i.Filtered.Value);
        }

        protected virtual void PerformSelection()
        {
            CarouselItem nextToSelect = GetNextToSelect();

            if (nextToSelect != null)
                nextToSelect.State.Value = CarouselItemState.Selected;
            else
                updateSelected(null);
        }

        private bool filteringChildren;

        public override void Filter(string[] textParts, CarouselFilter filter)
        {
            filteringChildren = true;
            base.Filter(textParts, filter);
            filteringChildren = false;

            attemptSelection();
        }

        private void updateSelected(CarouselItem newSelection)
        {
            if (newSelection != null)
                LastSelected = newSelection;
            updateSelectedIndex();
        }

        private void updateSelectedIndex() => lastSelectedIndex = LastSelected == null ? 0 : Math.Max(0, InternalChildren.IndexOf(LastSelected));
    }
}
