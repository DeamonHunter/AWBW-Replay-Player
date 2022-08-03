using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Layout;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Stats
{
    public class MultiLineGraph : Container
    {
        public float ActualMaxValue { get; private set; } = float.MinValue;
        public float ActualMinValue { get; private set; } = float.MaxValue;
        public int NumberOfValues { get; private set; } = 0;

        private readonly Container<Path> maskingContainer;
        private readonly List<Path> paths = new List<Path>();
        private readonly List<(float[], Color4)> pathValues = new List<(float[], Color4)>();

        private readonly LayoutValue pathCached = new LayoutValue(Invalidation.DrawSize);
        private const double transform_duration = 1500;

        public MultiLineGraph()
        {
            Add(maskingContainer = new Container<Path>()
            {
                Masking = true,
                RelativeSizeAxes = Axes.Both
            });

            AddLayout(pathCached);
        }

        protected override void Update()
        {
            base.Update();
            if (pathCached.IsValid)
                return;

            applyPaths();
            pathCached.Validate();
        }

        private void applyPaths()
        {
            foreach (var path in paths)
                path.ClearVertices();

            if (pathValues.Count <= 0)
                return;

            if (paths.Count < pathValues.Count)
            {
                for (int i = paths.Count; i < pathValues.Count; i++)
                {
                    var newPath = new SmoothPath()
                    {
                        AutoSizeAxes = Axes.None,
                        RelativeSizeAxes = Axes.Both,
                        PathRadius = 1f
                    };
                    maskingContainer.Add(newPath);
                    paths.Add(newPath);
                }
            }

            for (int i = 0; i < pathValues.Count; i++)
            {
                (var values, var colour) = pathValues[i];

                var path = paths[i];
                path.Colour = colour;

                for (int j = 0; j < values.Length; j++)
                {
                    float x = j / (float)(NumberOfValues - 1) * (DrawWidth - 2 * path.PathRadius);
                    float y = GetYPosition(values[j]) * (DrawHeight - 2 * path.PathRadius);
                    path.AddVertex(new Vector2(x, y));
                }
            }
        }

        protected float GetYPosition(float value)
        {
            if (ActualMinValue == ActualMaxValue)
                return value > 1 ? 0 : 1;

            return (ActualMaxValue - value) / (ActualMaxValue - ActualMinValue);
        }

        public void ClearPaths()
        {
            pathValues.Clear();

            ActualMaxValue = float.MinValue;
            ActualMinValue = float.MaxValue;
            NumberOfValues = 0;
            pathCached.Invalidate();
        }

        public void AddPath(Color4 colour, IEnumerable<float> values)
        {
            var array = values.ToArray();
            pathValues.Add((array, colour));

            var max = array.Max();
            var min = array.Min();

            ActualMaxValue = Math.Max(ActualMaxValue, max);
            ActualMinValue = Math.Min(ActualMinValue, min);
            NumberOfValues = Math.Max(NumberOfValues, array.Length);
            pathCached.Invalidate();

            if (pathValues.Count <= 1)
            {
                maskingContainer.ClearTransforms();
                maskingContainer.Width = 0;
                maskingContainer.ResizeWidthTo(1, transform_duration, Easing.OutQuint);
            }
        }
    }
}
