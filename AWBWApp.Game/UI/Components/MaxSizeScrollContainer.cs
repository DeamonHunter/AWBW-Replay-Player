using System;
using System.Collections.Generic;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Layout;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    /// <summary>
    /// Adds a Scroll Container around a <see cref="FillFlowContainer{T}"/> and allow setting a maximum size.
    /// </summary>
    public abstract class MaxSizeScrollContainer<T> : CompositeDrawable where T : Drawable
    {
        private float maxWidth = float.MaxValue;

        public float MaxWidth
        {
            get => maxWidth;
            set
            {
                if (Precision.AlmostEquals(maxWidth, value))
                    return;

                maxWidth = value;

                itemsFlow.SizeCache.Invalidate();
            }
        }

        private float maxHeight = float.PositiveInfinity;

        public float MaxHeight
        {
            get => maxHeight;
            set
            {
                if (Precision.AlmostEquals(maxHeight, value))
                    return;

                maxHeight = value;

                itemsFlow.SizeCache.Invalidate();
            }
        }

        public Color4 BackgroundColour
        {
            get => background.Colour;
            set => background.Colour = value;
        }

        public bool ScrollbarVisible
        {
            get => ContentContainer.ScrollbarVisible;
            set => ContentContainer.ScrollbarVisible = value;
        }

        public bool ScrollbarOverlapsContent
        {
            get => ContentContainer.ScrollbarOverlapsContent;
            set => ContentContainer.ScrollbarOverlapsContent = value;
        }

        protected readonly Direction Direction;
        protected FillFlowContainer<T> ItemsContainer => itemsFlow;
        protected internal IReadOnlyList<T> Children => ItemsContainer.Children;
        protected readonly ScrollContainer<Drawable> ContentContainer;
        protected readonly Container MaskingContainer;
        private readonly Box background;

        private SizeCacheFillFlowContainer<T> itemsFlow;

        public MaxSizeScrollContainer(Direction direction, SizeCacheFillFlowContainer<T> itemsFlow)
        {
            Direction = direction;
            this.itemsFlow = itemsFlow;

            InternalChildren = new Drawable[]
            {
                MaskingContainer = new Container
                {
                    Name = "Our contents",
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black
                        },
                        ContentContainer = CreateScrollContainer(direction).With(d =>
                        {
                            d.RelativeSizeAxes = Axes.Both;
                            d.Masking = false;
                            d.Child = itemsFlow;
                        })
                    }
                }
            };
        }

        protected abstract ScrollContainer<Drawable> CreateScrollContainer(Direction direction);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!itemsFlow.SizeCache.IsValid)
            {
                // Our children will be relatively-sized on the axis separate to the menu direction, so we need to compute
                // that size ourselves, based on the content size of our children, to give them a valid relative size

                float width = 0;
                float height = 0;

                foreach (var item in Children)
                {
                    width = Math.Max(width, item.DrawWidth);
                    height = Math.Max(height, item.DrawHeight);
                }

                // When scrolling in one direction, ItemsContainer is auto-sized in that direction and relative-sized in the other
                // In the case of the auto-sized direction, we want to use its size. In the case of the relative-sized direction, we want
                // to use the (above) computed size.
                width = Direction == Direction.Horizontal ? ItemsContainer.Width : width;
                height = Direction == Direction.Vertical ? (ItemsContainer.Height + 1) : height;

                width = Math.Min(MaxWidth, width);
                height = Math.Min(MaxHeight, height);

                // Regardless of the above result, if we are relative-sizing, just use the stored width/height
                width = RelativeSizeAxes.HasFlagFast(Axes.X) ? Width : width;
                height = RelativeSizeAxes.HasFlagFast(Axes.Y) ? Height : height;

                if (!Precision.AlmostEquals(Size, new Vector2(width, height), 0.1f))
                    Size = new Vector2(width, height);

                itemsFlow.SizeCache.Validate();
            }
        }

        public class SizeCacheFillFlowContainer<U> : FillFlowContainer<U> where U : Drawable
        {
            public readonly LayoutValue SizeCache = new LayoutValue(Invalidation.RequiredParentSizeToFit, InvalidationSource.Self);

            public SizeCacheFillFlowContainer()
            {
                AddLayout(SizeCache);
            }
        }
    }
}
