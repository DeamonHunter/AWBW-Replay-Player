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
        public float ActualMaxValue { get; private set; } = 0;
        public float ActualMinValue { get; private set; } = 0;

        public float ShownMaxValue => Math.Max(0, pathValues.Max(x => x.Active ? x.MaxValue : 0));
        public float ShownMinValue => Math.Min(0, pathValues.Min(x => x.Active ? x.MaxValue : 0));

        public int NumberOfValues { get; private set; } = 0;
        public int NumberOfLines => pathValues.Count;

        private readonly Container<Path> maskingContainer;
        private readonly List<Path> paths = new List<Path>();
        private readonly List<LineData> pathValues = new List<LineData>();

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
                        PathRadius = 1f,
                    };
                    maskingContainer.Add(newPath);
                    paths.Add(newPath);
                }
            }

            var maxValue = ShownMaxValue;
            var minValue = ShownMinValue;

            for (int i = 0; i < pathValues.Count; i++)
            {
                var data = pathValues[i];
                if (!data.Active)
                    continue;

                var path = paths[i];
                path.Colour = data.DisplayColor;

                if (NumberOfValues == 1)
                {
                    path.AddVertex(new Vector2(0, 0));
                    path.AddVertex(new Vector2((DrawWidth - 2 * path.PathRadius), 0));
                }
                else
                {
                    for (int j = 0; j < data.Values.Length; j++)
                    {
                        float x = j / (float)(NumberOfValues - 1) * (DrawWidth - 2 * path.PathRadius);
                        float y = GetYPosition(data.Values[j], maxValue, minValue) * (DrawHeight - 2 * path.PathRadius);
                        path.AddVertex(new Vector2(x, y));
                    }
                }
            }
        }

        protected float GetYPosition(float value, float maxValue, float minValue)
        {
            if (maxValue == minValue)
            {
                minValue -= 1;
                maxValue += 1;
            }

            return (maxValue - value) / (maxValue - minValue);
        }

        public void ClearPaths()
        {
            pathValues.Clear();

            ActualMaxValue = 0;
            ActualMinValue = 0;
            NumberOfValues = 0;
            pathCached.Invalidate();
        }

        public void AddPath(Color4 colour, IEnumerable<float> values)
        {
            var array = values.ToArray();

            var max = array.Max();
            var min = array.Min();

            pathValues.Add(new LineData { Values = array, Active = true, DisplayColor = colour, MaxValue = max, MinValue = min });

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

        public void SetVisibilityOfLine(int lineNum, bool visible)
        {
            pathValues[lineNum].Active = visible;
            pathCached.Invalidate();
        }

        public bool GetVisibilityOfLine(int lineNum) => pathValues[lineNum].Active;

        private class LineData
        {
            public float[] Values;
            public float MaxValue;
            public float MinValue;
            public Color4 DisplayColor;
            public bool Active;
        }
    }
}
