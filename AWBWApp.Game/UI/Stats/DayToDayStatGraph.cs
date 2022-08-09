using System;
using System.Collections.Generic;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Layout;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Stats
{
    public class DayToDayStatGraph : Container
    {
        private readonly Box background;
        private Container<SpriteText> rowTicksContainer;
        private Container<SpriteText> columnTicksContainer;
        private Container<Box> rowLinesContainer;
        private Container<Box> columnLinesContainer;

        private readonly LayoutValue pathCached = new LayoutValue(Invalidation.RequiredParentSizeToFit);
        private readonly MultiLineGraph graph;
        private readonly FillFlowContainer<LegendButton> legend;

        public int PlayerCount = 2;

        public DayToDayStatGraph()
        {
            Masking = true;
            CornerRadius = 5;
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                background = new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(200, 200, 200, 255)
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,

                    Children = new Drawable[]
                    {
                        new GridContainer()
                        {
                            RelativeSizeAxes = Axes.X,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Size = new Vector2(0.95f, 200),
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension()
                            },
                            RowDimensions = new[]
                            {
                                new Dimension(),
                                new Dimension(GridSizeMode.AutoSize),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    rowTicksContainer = new Container<SpriteText>
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        AutoSizeAxes = Axes.X
                                    },
                                    new Container()
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new Container()
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Children = new[]
                                                {
                                                    rowLinesContainer = new Container<Box> { RelativeSizeAxes = Axes.Both },
                                                    columnLinesContainer = new Container<Box> { RelativeSizeAxes = Axes.Both },
                                                }
                                            },
                                            graph = new MultiLineGraph { RelativeSizeAxes = Axes.Both }
                                        }
                                    }
                                },
                                new Drawable[]
                                {
                                    Empty(),
                                    columnTicksContainer = new Container<SpriteText>()
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Padding = new MarginPadding { Top = 5, Right = 5 }
                                    }
                                }
                            }
                        },
                        legend = new FillFlowContainer<LegendButton>()
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Full,
                            Spacing = new Vector2(2, 0)
                        }
                    }
                }
            };

            AddLayout(pathCached);
        }

        public void ClearPaths()
        {
            graph.ClearPaths();
            legend.Clear();
            pathCached.Invalidate();
        }

        public void AddPath(string legendName, Color4 colour, IEnumerable<float> values)
        {
            var lineNumber = graph.NumberOfLines;
            graph.AddPath(colour, values);
            legend.Add(new LegendButton(colour)
            {
                Text = legendName,
                Action = () => setVisibilityOfLine(lineNumber)
            });
            pathCached.Invalidate();
        }

        private void setVisibilityOfLine(int lineNumber)
        {
            var active = !graph.GetVisibilityOfLine(lineNumber);
            graph.SetVisibilityOfLine(lineNumber, active);
            legend[lineNumber].SetActive(active);
            pathCached.Invalidate();
        }

        protected override void Update()
        {
            base.Update();
            if (pathCached.IsValid)
                return;

            createRowTicks();
            createColumnTicks();
            pathCached.Validate();
        }

        private void createRowTicks()
        {
            rowTicksContainer.Clear();
            rowLinesContainer.Clear();

            if (graph.NumberOfValues <= 0)
                return;

            var min = (long)graph.ShownMinValue;
            var max = (long)graph.ShownMaxValue;

            if (min == max)
            {
                min -= 1;
                max += 1;
            }

            var tickInterval = getTickInterval(max - min, 6);

            for (long currentTick = 0; currentTick <= max; currentTick += tickInterval)
            {
                float y;
                if (min == max)
                    y = currentTick > 1 ? 1 : 0;
                else
                    y = Interpolation.ValueAt(Math.Max(currentTick, min), 0, 1f, min, max);

                // y axis is inverted in graph-like coordinates.
                addRowTick(-y, currentTick);
            }

            for (long currentTick = -tickInterval; currentTick >= min; currentTick -= tickInterval)
            {
                float y;
                if (min == max)
                    y = currentTick > 1 ? 1 : 0;
                else
                    y = Interpolation.ValueAt(Math.Max(currentTick, min), 0, 1f, min, max);

                // y axis is inverted in graph-like coordinates.
                addRowTick(-y, currentTick);
            }
        }

        private void createColumnTicks()
        {
            columnTicksContainer.Clear();
            columnLinesContainer.Clear();

            if (graph.NumberOfValues <= 1)
                return;

            var turnsPerTick = PlayerCount * 5;

            for (int i = 0; i <= graph.NumberOfValues; i += turnsPerTick)
            {
                float x = (float)i / graph.NumberOfValues;
                addColumnTick(x, i / PlayerCount);
            }

            if (graph.NumberOfValues % turnsPerTick != 0)
                addColumnTick(1, ((graph.NumberOfValues + 1) / PlayerCount));
        }

        private long getTickInterval(long range, int maxTicksCount)
        {
            var exactTickInterval = (float)range / (maxTicksCount - 1);

            double numberOfDigits = Math.Floor(Math.Log10(exactTickInterval));
            double tickBase = Math.Pow(10, numberOfDigits);

            double exactTickMultiplier = exactTickInterval / tickBase;

            double tickMultiplier;
            if (exactTickMultiplier < 1.5)
                tickMultiplier = 1.0;
            else if (exactTickMultiplier < 3)
                tickMultiplier = 2.0;
            else if (exactTickMultiplier < 7)
                tickMultiplier = 5.0;
            else
                tickMultiplier = 10.0;

            return Math.Max((long)(tickMultiplier * tickBase), 1);
        }

        private void addRowTick(float y, double value)
        {
            rowTicksContainer.Add(new SpriteText()
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.CentreRight,
                RelativePositionAxes = Axes.Y,
                Margin = new MarginPadding { Right = 3 },
                Text = value.ToLocalisableString("N0"),
                Colour = new Color4(20, 20, 20, 255),
                Font = FontUsage.Default.With(size: 12),
                Y = Math.Max(y, -0.98f)
            });

            rowLinesContainer.Add(new Box()
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.CentreRight,
                RelativeSizeAxes = Axes.X,
                RelativePositionAxes = Axes.Y,
                Height = value == 0 ? 0.5f : 0.1f,
                Colour = value == 0 ? new Color4(20, 20, 20, 255) : new Color4(20, 20, 20, 100),
                EdgeSmoothness = Vector2.One,
                Y = y
            });
        }

        private void addColumnTick(float x, int value)
        {
            columnTicksContainer.Add(new SpriteText
            {
                Origin = Anchor.CentreLeft,
                RelativePositionAxes = Axes.X,
                Text = value.ToString(),
                Font = FontUsage.Default.With(size: 12),
                Colour = new Color4(20, 20, 20, 255),
                Rotation = 15,
                X = Math.Min(x, 0.98f)
            });

            columnLinesContainer.Add(new Box
            {
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.Y,
                RelativePositionAxes = Axes.X,
                Width = value == 0 ? 0.5f : 0.1f,
                Colour = value == 0 ? new Color4(20, 20, 20, 255) : new Color4(20, 20, 20, 100),
                EdgeSmoothness = Vector2.One,
                X = x
            });
        }

        private class LegendButton : BasicButton
        {
            private Box activeBox;

            public LegendButton(Color4 lineColour)
            {
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;
                AutoSizeAxes = Axes.X;
                Height = 20;
                BackgroundColour = new Color4(200, 200, 200, 255);

                SpriteText.Position = new Vector2(8, 0);
                SpriteText.Colour = new Color4(20, 20, 20, 255);
                SpriteText.Font = SpriteText.Font.With(size: 14);
                AddRange(new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(2, 0),
                        Colour = lineColour,
                        Size = new Vector2(12, 3)
                    },
                    activeBox = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(20, 20, 20, 50),
                        Alpha = 0
                    }
                });
            }

            public void SetActive(bool active)
            {
                activeBox.FadeTo(active ? 0 : 1, 150, Easing.OutQuint);
            }
        }
    }
}
