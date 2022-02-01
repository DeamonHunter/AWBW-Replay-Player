using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace AWBWApp.Game.UI
{
    public class MapCameraController : CompositeDrawable
    {
        public int MinScale { get; set; } = 1;
        public int MaxScale { get; set; } = 1;
        public MarginPadding MapSpace { get; set; }

        public MapCameraController(Drawable child)
        {
            InternalChild = child;
            child.AlwaysPresent = true;
        }

        //Todo: Center it as well
        public void FitMapToSpace()
        {
            var drawSize = new Vector2(DrawSize.X - MapSpace.TotalHorizontal, DrawSize.Y - MapSpace.TotalVertical);

            var possibleScaleX = drawSize.X / (InternalChild.Size.X / InternalChild.Scale.X);
            var possibleScaleY = drawSize.Y / (InternalChild.Size.Y / InternalChild.Scale.Y);

            var newScale = Math.Max(MinScale, Math.Min(possibleScaleX, possibleScaleY) * 0.975f);
            InternalChild.Scale = new Vector2(newScale);
            InternalChild.Position = new Vector2(MapSpace.Left, MapSpace.Top);
        }

        protected override bool OnScroll(ScrollEvent e)
        {
            var cursorPosition = ToParentSpace(e.MousePosition);
            var offset = InternalChild.AnchorPosition + InternalChild.Position;

            var cursorOffsetFromCenter = cursorPosition - offset;

            var scale = Vector2.Clamp(InternalChild.Scale * new Vector2(1 + e.ScrollDelta.Y * 0.075f), new Vector2(MinScale), new Vector2(MaxScale));
            if (scale == InternalChild.Scale)
                return base.OnScroll(e);

            var originalScale = InternalChild.Scale;
            InternalChild.Scale = scale;
            InternalChild.Position -= cursorOffsetFromCenter * (new Vector2(scale.X / originalScale.X, scale.Y / originalScale.Y) - Vector2.One);

            return base.OnScroll(e);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Middle)
                return base.OnDragStart(e);

            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            if (e.Button != MouseButton.Middle)
            {
                base.OnDrag(e);
                return;
            }

            InternalChild.Position += e.Delta;
        }
    }
}
