using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace AWBWApp.Game.UI
{
    public class MapCameraController : CompositeDrawable
    {
        public int MaxScale { get; set; } = 1;

        public MapCameraController(Drawable child)
        {
            InternalChild = child;
            child.AlwaysPresent = true;
        }

        protected override bool OnScroll(ScrollEvent e)
        {
            var scale = (InternalChild.Scale + new Vector2(e.ScrollDelta.Y));
            InternalChild.Scale = Vector2.Clamp(scale, Vector2.One, new Vector2(MaxScale));
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
