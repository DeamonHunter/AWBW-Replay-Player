using System.Diagnostics;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Input.Events;

namespace AWBWApp.Game.UI.Select
{
    public abstract class DrawableCarouselItem : PoolableDrawable
    {
        public const float MAX_HEIGHT = 100;

        public override bool IsPresent => base.IsPresent || Item?.Visible == true;

        public readonly CarouselHeader Header;

        protected readonly Container MovementContainer;
        protected readonly Container<Drawable> Content;

        public CarouselItem Item
        {
            get => item;
            set
            {
                if (item == value)
                    return;

                if (item != null)
                {
                    item.Filtered.ValueChanged -= onStateChange;
                    item.State.ValueChanged -= onStateChange;

                    Header.State.UnbindFrom(item.State);

                    if (item is CarouselGroup group)
                    {
                        foreach (var c in group.Children)
                            c.Filtered.ValueChanged -= onStateChange;
                    }
                }

                item = value;

                if (IsLoaded)
                    UpdateItem();
            }
        }

        private CarouselItem item;

        protected DrawableCarouselItem()
        {
            RelativeSizeAxes = Axes.X;
            Alpha = 0;

            InternalChildren = new Drawable[]
            {
                MovementContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        Header = new CarouselHeader(),
                        Content = new Container()
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            UpdateItem();
        }

        protected override void Update()
        {
            base.Update();
            Content.Y = Header.Height; //Todo: Hmmm
        }

        protected virtual void UpdateItem()
        {
            if (item == null)
                return;

            Scheduler.AddOnce(ApplyState);

            Item.Filtered.ValueChanged += onStateChange;
            Item.State.ValueChanged += onStateChange;

            Header.State.BindTo(Item.State);

            if (Item is CarouselGroup group)
            {
                foreach (var c in group.Children)
                    c.Filtered.ValueChanged += onStateChange;
            }
        }

        private void onStateChange(ValueChangedEvent<CarouselItemState> _) => Scheduler.AddOnce(ApplyState);
        private void onStateChange(ValueChangedEvent<bool> _) => Scheduler.AddOnce(ApplyState);

        protected virtual void ApplyState()
        {
            //Uses the fact that we know the height of items to avoid autosizing overhead.
            Height = Item.TotalHeight;

            Debug.Assert(Item != null);

            switch (Item.State.Value)
            {
                case CarouselItemState.NotSelected:
                    Deselected();
                    break;

                case CarouselItemState.Selected:
                    Selected();
                    break;
            }

            if (Item.Visible)
                this.FadeIn(250);
            else
                this.FadeOut(300, Easing.OutQuint);
        }

        protected virtual void Selected()
        {
            Debug.Assert(Item != null);
        }

        protected virtual void Deselected()
        {
        }

        protected override bool OnClick(ClickEvent e)
        {
            Item.State.Value = CarouselItemState.Selected;
            return true;
        }

        public void SetMultiplicativeAlpha(float alpha) => Header.BorderContainer.Alpha = alpha;
    }
}
