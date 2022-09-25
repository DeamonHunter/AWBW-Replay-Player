using System;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace AWBWApp.Game.UI.Components
{
    /// <summary>
    /// This is a container that will take a bunch of fill flow children and attempt to fit them inside a vertical space. As the difference gets worse, this will take more time to settle.
    /// </summary>
    public class ShrinkingNamesContainer : Container
    {
        private readonly FillFlowContainer childDrawable;

        private float previousScale = 1;
        private Vector2 prevChildSize;

        public ShrinkingNamesContainer(FillFlowContainer child)
        {
            childDrawable = child;
            AddInternal(childDrawable);
        }

        protected override void Update()
        {
            base.Update();

            if (prevChildSize == childDrawable.Size)
                return;

            prevChildSize = childDrawable.Size;

            //Todo: Does this cause problems if the drawable is scaled?
            var drawSize = DrawSize - new Vector2(Padding.Left + Padding.Right, Padding.Bottom + Padding.Top);
            if (drawSize.X <= 0 || drawSize.Y <= 0)
                return;

            var absDiff = Math.Abs(prevChildSize.Y - drawSize.Y);
            if (prevChildSize.Y < drawSize.Y && absDiff < drawSize.Y / 2.5f)
                return;

            var newSize = 0.6f * (absDiff / drawSize.Y);

            if (prevChildSize.Y < drawSize.Y)
            {
                if (previousScale >= 1)
                    return;

                previousScale = Math.Min(1, previousScale + newSize);
            }
            else
            {
                if (previousScale <= 0)
                    return;

                previousScale = Math.Max(0, previousScale - newSize);
            }

            foreach (var drawable in childDrawable)
                drawable.Scale = new Vector2(previousScale);
        }
    }
}
