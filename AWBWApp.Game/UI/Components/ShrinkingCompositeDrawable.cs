using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace AWBWApp.Game.UI.Components
{
    public class ShrinkingCompositeDrawable : CompositeDrawable
    {
        public Axes ShrinkAxes
        {
            get => shrinkAxes;
            set
            {
                shrinkAxes = value;
                prevChildSize = new Vector2(float.MinValue); // Todo: Check out cache invalidation to see if I can do this a bit smarter
            }
        }

        private Axes shrinkAxes = Axes.Both;

        private readonly Drawable childDrawable;

        private Vector2 prevDrawSize;
        private Vector2 prevChildSize;

        public ShrinkingCompositeDrawable(Drawable child)
        {
            childDrawable = child;
            AddInternal(childDrawable);
        }

        protected override void Update()
        {
            base.Update();

            //Todo: Does this cause problems if the drawable is scaled?
            var drawSize = DrawSize - new Vector2(Padding.Left + Padding.Right, Padding.Bottom + Padding.Top);
            var childSize = childDrawable.Size;

            if (prevDrawSize == drawSize && prevChildSize == childSize)
                return;

            if (drawSize.X > 0 && drawSize.Y > 0)
            {
                float scale;

                switch (ShrinkAxes)
                {
                    case Axes.Both:
                        scale = Math.Min(drawSize.X / childSize.X, drawSize.Y / childSize.Y);
                        break;

                    case Axes.X:
                        scale = drawSize.X / childSize.X;
                        break;

                    case Axes.Y:
                        scale = drawSize.Y / childSize.Y;
                        break;

                    default:
                        scale = 1;
                        break;
                }

                childDrawable.Scale = new Vector2(Math.Min(scale, 1));
            }
            else
                childDrawable.Scale = Vector2.One;

            prevDrawSize = drawSize;
            prevChildSize = childSize;
        }
    }
}
