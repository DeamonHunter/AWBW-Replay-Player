using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class UnitRangeIndicator : Container
    {
        private readonly Container<Box> primaryBoxContainer;
        private readonly Container<Path> primaryOutlineContainer;

        private readonly Container<Box> secondaryBoxContainer;
        private readonly Container<Path> secondaryOutlineContainer;

        private Vector2I lastCenter;

        public UnitRangeIndicator()
        {
            Children = new Drawable[]
            {
                secondaryBoxContainer = new Container<Box>(),
                secondaryOutlineContainer = new Container<Path>(),
                primaryBoxContainer = new Container<Box>(),
                primaryOutlineContainer = new Container<Path>()
            };
        }

        public void ShowNewRange(List<Vector2I> positions, Vector2I center, Color4 colour, Color4 outlineColour, bool secondary)
        {
            Position = GameMap.GetDrawablePositionForBottomOfTile(center);

            var box = secondary ? secondaryBoxContainer : primaryBoxContainer;
            var line = secondary ? secondaryOutlineContainer : primaryOutlineContainer;

            if (Alpha > 0.75f && lastCenter == center)
            {
                foreach (var child in box.Children)
                    child.FadeOut(500, Easing.OutCubic).Expire();
                foreach (var child in line.Children)
                    child.FadeOut(500, Easing.OutCubic).Expire();
            }
            else
            {
                box.Clear();
                line.Clear();
            }

            lastCenter = center;

            showRange(box, line, positions, center, colour, outlineColour);
        }

        public void ClearSecondaryRange()
        {
            if (Alpha > 0.75f)
            {
                foreach (var child in secondaryBoxContainer.Children)
                    child.FadeOut(500, Easing.OutCubic).Expire();
                foreach (var child in secondaryOutlineContainer.Children)
                    child.FadeOut(500, Easing.OutCubic).Expire();
            }
            else
            {
                secondaryBoxContainer.Clear();
                secondaryOutlineContainer.Clear();
            }
        }

        private void showRange(Container<Box> boxContainer, Container<Path> pathContainer, List<Vector2I> positions, Vector2I center, Color4 colour, Color4 outlineColour)
        {
            positions.Sort((x, y) =>
            {
                var yComparison = x.Y.CompareTo(y.Y);
                if (yComparison != 0)
                    return yComparison;

                return x.X.CompareTo(y.X);
            });

            var edges = new HashSet<(Vector2I, Vector2I)>();

            foreach (var position in positions)
            {
                var x = position.X - center.X;
                var y = position.Y - center.Y;

                var topEdge = (new Vector2I(x, y), new Vector2I(x + 1, y));
                if (edges.Contains(topEdge))
                    edges.Remove(topEdge);
                else
                    edges.Add(topEdge);

                var rightEdge = (new Vector2I(x + 1, y), new Vector2I(x + 1, y + 1));
                if (edges.Contains(rightEdge))
                    edges.Remove(rightEdge);
                else
                    edges.Add(rightEdge);

                var bottomEdge = (new Vector2I(x, y + 1), new Vector2I(x + 1, y + 1));
                if (edges.Contains(bottomEdge))
                    edges.Remove(bottomEdge);
                else
                    edges.Add(bottomEdge);

                var leftEdge = (new Vector2I(x, y), new Vector2I(x, y + 1));
                if (edges.Contains(leftEdge))
                    edges.Remove(leftEdge);
                else
                    edges.Add(leftEdge);

                boxContainer.Add(new Box
                {
                    Colour = colour,
                    Size = DrawableTile.BASE_SIZE,
                    Position = new Vector2(DrawableTile.BASE_SIZE.X * x, DrawableTile.BASE_SIZE.Y * y)
                });
            }

            //Todo: Is there a more efficient algorithm for this.
            while (edges.Count > 0)
            {
                var outline = new Path
                {
                    PathRadius = 1,
                    Colour = outlineColour
                };

                var nextEdge = edges.First();
                var startPoint = nextEdge.Item1;
                outline.AddVertex(new Vector2(startPoint.X * DrawableTile.BASE_SIZE.X, startPoint.Y * DrawableTile.BASE_SIZE.Y));

                var point = nextEdge.Item2;
                var direction = point.X - startPoint.X != 0 ? PointDirection.PosX : PointDirection.PosY; //Due to how we construct the edges, they were only in the positive direction

                var minX = 0;
                var minY = 0;

                while (true)
                {
                    minX = Math.Min(nextEdge.Item1.X, minX);
                    minY = Math.Min(nextEdge.Item1.Y, minY);

                    edges.Remove(nextEdge);
                    outline.AddVertex(new Vector2(point.X * DrawableTile.BASE_SIZE.X, point.Y * DrawableTile.BASE_SIZE.Y));
                    if (point == startPoint)
                        break;

                    nextEdge = (point, point + new Vector2I(1, 0));

                    if (direction != PointDirection.NegX && edges.Contains(nextEdge))
                    {
                        point = nextEdge.Item2;
                        direction = PointDirection.PosX;
                        continue;
                    }

                    nextEdge = (point - new Vector2I(1, 0), point);

                    if (direction != PointDirection.PosX && edges.Contains(nextEdge))
                    {
                        point = nextEdge.Item1;
                        direction = PointDirection.NegX;
                        continue;
                    }

                    nextEdge = (point, point + new Vector2I(0, 1));

                    if (direction != PointDirection.NegY && edges.Contains(nextEdge))
                    {
                        point = nextEdge.Item2;
                        direction = PointDirection.PosY;
                        continue;
                    }

                    nextEdge = (point - new Vector2I(0, 1), point);

                    if (direction != PointDirection.PosY && edges.Contains(nextEdge))
                    {
                        point = nextEdge.Item1;
                        direction = PointDirection.NegY;
                        continue;
                    }

                    throw new Exception("Missing edge.");
                }

                outline.Position = new Vector2(minX * DrawableTile.BASE_SIZE.X, minY * DrawableTile.BASE_SIZE.Y);

                pathContainer.Add(outline);
            }

            this.FadeIn(500, Easing.OutQuint);
        }

        private enum PointDirection
        {
            None,
            PosX,
            PosY,
            NegX,
            NegY
        }
    }
}
