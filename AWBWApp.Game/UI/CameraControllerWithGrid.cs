using System;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.UI.Components;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI
{
    public class CameraControllerWithGrid : Container
    {
        public int MinScale { get; set; } = 1;
        public int MaxScale { get; set; } = 1;
        public MarginPadding MapSpace { get; set; }
        public MarginPadding MovementRegion { get; set; }

        protected override Container<Drawable> Content => content;

        private Container content;

        private Box background;
        private MovingGrid grid;

        private static readonly Vector2 grid_offset = new Vector2(-1, -2);

        private IBindable<bool> allowLeftMouseToDragMap;
        private IBindable<Colour4> baseMapColour;
        private IBindable<Colour4> baseGridColour;

        public CameraControllerWithGrid()
        {
            InternalChildren = new Drawable[]
            {
                background = new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(42, 91, 139, 255).Lighten(0.2f),
                },
                grid = new MovingGrid()
                {
                    Spacing = new Vector2(16),
                    GridOffset = new Vector2(-1, -2),
                    LineSize = new Vector2(2),
                    RelativeSizeAxes = Axes.Both,
                    GridColor = new Color4(42, 91, 139, 255).Darken(0.8f),
                    Velocity = Vector2.Zero
                },
                content = new Container()
                {
                    AutoSizeAxes = Axes.Both
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager)
        {
            allowLeftMouseToDragMap = configManager.GetBindable<bool>(AWBWSetting.ReplayAllowLeftMouseToDragMap);

            baseMapColour = configManager.GetBindable<Colour4>(AWBWSetting.MapGridBaseColour);
            baseMapColour.BindValueChanged(x => background.Colour = x.NewValue, true);

            baseGridColour = configManager.GetBindable<Colour4>(AWBWSetting.MapGridGridColour);
            baseGridColour.BindValueChanged(x => grid.GridColor = x.NewValue, true);
        }

        //Todo: Center it as well
        public void FitMapToSpace()
        {
            var drawSize = new Vector2(DrawSize.X - MapSpace.TotalHorizontal, DrawSize.Y - MapSpace.TotalVertical);

            var contentSize = content.Size;

            var possibleScaleX = drawSize.X / contentSize.X;
            var possibleScaleY = drawSize.Y / contentSize.Y;

            var newScale = Math.Clamp(Math.Min(possibleScaleX, possibleScaleY) * 0.975f, MinScale, MaxScale);

            contentSize *= newScale;

            var offset = (drawSize - contentSize) * 0.5f;

            content.Scale = new Vector2(newScale);

            moveMapToPosition(new Vector2(MapSpace.Left, MapSpace.Top) + offset);
        }

        protected override bool OnScroll(ScrollEvent e)
        {
            var cursorPosition = ToParentSpace(e.MousePosition);
            var offset = content.AnchorPosition + content.Position;

            var cursorOffsetFromCenter = cursorPosition - offset;

            var scale = Vector2.Clamp(content.Scale * new Vector2(1 + e.ScrollDelta.Y * 0.075f), new Vector2(MinScale), new Vector2(MaxScale));
            if (scale == content.Scale)
                return base.OnScroll(e);

            var originalScale = content.Scale;
            content.Scale = scale;
            moveMapToPosition(content.Position - cursorOffsetFromCenter * (new Vector2(scale.X / originalScale.X, scale.Y / originalScale.Y) - Vector2.One));

            return base.OnScroll(e);
        }

        private void updateGrid()
        {
            grid.Scale = content.Scale;
            grid.GridOffset = new Vector2(content.Position.X / content.Scale.X, content.Position.Y / content.Scale.Y) + grid_offset;
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (!allowLeftMouseToDragMap.Value)
                    return base.OnDragStart(e);
            }
            else if (e.Button != MouseButton.Middle && e.Button != MouseButton.Right)
                return base.OnDragStart(e);

            return true;
        }

        private void moveMapToPosition(Vector2 position)
        {
            var additionalAdjustment = DrawableTile.BASE_SIZE * (content.Scale - Vector2.One); //Todo: Fix the maths below so this isn't as necessary

            var upperLeftBounds = new Vector2((MovementRegion.Left - content.Size.X * content.Scale.X), MovementRegion.Top - content.Size.Y * content.Scale.Y) + additionalAdjustment;
            var lowerRightBounds = new Vector2(DrawSize.X - MovementRegion.Right, DrawSize.Y - MovementRegion.Bottom) - additionalAdjustment;

            //Clamp the position so that it remains on screen.
            content.Position = new Vector2(Math.Clamp(position.X, upperLeftBounds.X, lowerRightBounds.X), Math.Clamp(position.Y, upperLeftBounds.Y, lowerRightBounds.Y));
            updateGrid();
        }

        protected override void OnDrag(DragEvent e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (!allowLeftMouseToDragMap.Value)
                {
                    base.OnDrag(e);
                    return;
                }
            }
            else if (e.Button != MouseButton.Middle && e.Button != MouseButton.Right)
            {
                base.OnDrag(e);
                return;
            }

            moveMapToPosition(content.Position + e.Delta);
        }
    }
}
