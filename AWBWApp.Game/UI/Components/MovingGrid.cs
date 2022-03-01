using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Layout;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    public class MovingGrid : CompositeDrawable
    {
        private LayoutValue gridCache = new LayoutValue(Invalidation.RequiredParentSizeToFit);

        private Vector2 spacing = Vector2.One;

        public Vector2 Spacing
        {
            get => spacing;
            set
            {
                if (spacing.X <= 0 || spacing.Y <= 0)
                    throw new ArgumentException("Grid spacing must be positive and non-zero.");

                spacing = value;
                gridCache.Invalidate();
            }
        }

        private Vector2 lineSize = new Vector2(3);

        public Vector2 LineSize
        {
            get => lineSize;
            set
            {
                if (lineSize.X <= 0 || lineSize.Y <= 0)
                    throw new ArgumentException("Line Size must be positive and non-zero.");

                lineSize = value;
                gridCache.Invalidate();
            }
        }

        private Color4 gridColor;

        public Color4 GridColor
        {
            get => gridColor;
            set
            {
                gridColor = value;
                gridCache.Invalidate();
            }
        }

        public Vector2 Velocity = new Vector2(1, 0);

        private Vector2 gridOffset;

        public Vector2 GridOffset
        {
            get => gridOffset;
            set
            {
                gridOffset = new Vector2(value.X % Spacing.X, value.Y % Spacing.Y);
                gridCache.Invalidate();
            }
        }

        private Vector2 gridSize;

        public MovingGrid()
        {
            AddLayout(gridCache);
        }

        protected override void Update()
        {
            base.Update();

            if (!gridCache.IsValid)
            {
                ClearInternal();
                createGrid();
                gridCache.Validate();
                return;
            }

            var distanceMoved = Velocity * (float)Clock.ElapsedFrameTime * 0.001f;
            gridOffset = new Vector2((gridOffset.X + distanceMoved.X) % Spacing.X, (gridOffset.Y + distanceMoved.Y) % Spacing.Y);
            moveGridByDistance(distanceMoved);
        }

        private void createGrid()
        {
            var drawSize = DrawSize;

            generateLines(Direction.Horizontal, 0, drawSize.Y, Spacing.Y);
            generateLines(Direction.Vertical, 0, drawSize.X, Spacing.X);

            gridSize = new Vector2(MathF.Ceiling(drawSize.X / Spacing.X) * Spacing.X, MathF.Ceiling(drawSize.Y / Spacing.Y) * Spacing.Y);
            moveGridByDistance(GridOffset);
        }

        private void generateLines(Direction direction, float startPosition, float endPosition, float step)
        {
            int index = 0;

            float currentPostion = startPosition;

            while ((endPosition - currentPostion) * Math.Sign(step) > 0)
            {
                var gridLine = new Box()
                {
                    Colour = GridColor,
                    //Alpha = index == 0 ? 0.3f : 0.1f,
                    Alpha = 0.1f,
                    EdgeSmoothness = new Vector2(0.2f)
                };

                if (direction == Direction.Horizontal)
                {
                    gridLine.RelativeSizeAxes = Axes.X;
                    gridLine.Height = LineSize.Y;
                    gridLine.Y = currentPostion;
                }
                else
                {
                    gridLine.RelativeSizeAxes = Axes.Y;
                    gridLine.Width = LineSize.X;
                    gridLine.X = currentPostion;
                }

                AddInternal(gridLine);

                index++;
                currentPostion = startPosition + index * step;
            }
        }

        private void moveGridByDistance(Vector2 distance)
        {
            foreach (var child in InternalChildren)
            {
                var newPosition = child.Position + distance;

                if (child.RelativeSizeAxes == Axes.X)
                {
                    newPosition.X = 0;
                    if (newPosition.Y > gridSize.Y)
                        newPosition.Y -= gridSize.Y;
                    if (newPosition.Y < 0)
                        newPosition.Y += gridSize.Y;
                }
                else
                {
                    newPosition.Y = 0;

                    if (newPosition.X < 0)
                        newPosition.X += gridSize.X;

                    if (newPosition.X > gridSize.X)
                        newPosition.X -= gridSize.X;
                }

                child.Position = newPosition;
            }
        }
    }
}
