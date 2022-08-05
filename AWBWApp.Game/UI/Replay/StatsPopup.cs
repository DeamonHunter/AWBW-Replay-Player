using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Components.Tooltip;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class StatsPopup : TooltipContainer, IHasContextMenu
    {
        public long PlayerID;

        private StatScrollContainer scrollContainer;

        private Dictionary<long, PlayerInfo> players;
        private Action<long, long> compareToAction;

        public StatsPopup(Dictionary<long, PlayerInfo> players, long playerID, PlayerStatsReadout readout, Action<long, long> compareToAction)
        {
            PlayerID = playerID;
            this.players = players;
            this.compareToAction = compareToAction;

            AutoSizeAxes = Axes.Y;

            Width = 300;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            Masking = true;
            CornerRadius = 8;

            var headerColor = Color4Extensions.FromHex(players[playerID].Country.Value.Colours["playerList"]).Darken(0.1f);
            var borderColor = headerColor.Darken(0.2f);

            var cachedSizeFlowContainer = new MaxSizeScrollContainer<Drawable>.SizeCacheFillFlowContainer<Drawable>()
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new Drawable[]
                {
                    new StatLine("Total Generated Funds", readout.GeneratedMoney)
                    {
                        Tooltip = $"Spent on Building: {readout.MoneySpentOnBuildingUnits}\nSpent On Repairing: {readout.MoneySpentOnRepairingUnits}"
                    },
                    new StatLine("Powers Used", $"{readout.PowersUsed} COP / {readout.SuperPowersUsed} SCOP"),
                    new UnitFlowContainer("Built/Value", players[playerID].Country.Value.UnitPath, readout.BuildStats, "Built Units/Built Unit Value", readout.TotalCountBuilt, readout.TotalValueBuilt),
                    new UnitFlowContainer("Deaths/Value Damage Taken", players[playerID].Country.Value.UnitPath, readout.LostStats, "Lost Units/Unit Value Lost", readout.TotalCountLost, readout.TotalValueLost),
                    new UnitFlowContainer("Kills/Value Damage Dealt", players, readout.DamageOtherStats, "Killed Units/Unit Value Damage", readout.TotalCountDamaged, readout.TotalValueDamaged),
                }
            };

            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(230, 230, 230, 255)
                },
                new EndGamePlayerListItem(players[playerID], false, headerColor, borderColor, Color4.White, false)
                {
                    Size = new Vector2(1, 40),
                    Position = Vector2.Zero
                },
                new StandardCloseButton()
                {
                    ButtonColour = Color4.White,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Padding = new MarginPadding { Top = 10, Right = 5 },
                    Action = Close
                },
                scrollContainer = new StatScrollContainer(Direction.Vertical, cachedSizeFlowContainer)
                {
                    BackgroundColour = Color4.Transparent,
                    Position = new Vector2(0, 40),
                    RelativeSizeAxes = Axes.X,
                    MaxHeight = 400
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Open();
        }

        public void Open()
        {
            scrollContainer.ScrollbarVisible = false;
            scrollContainer.ScrollbarOverlapsContent = true;
            this.FadeInFromZero(300, Easing.OutQuint);
            this.ScaleTo(new Vector2(0.5f, 0f)).ScaleTo(new Vector2(1.15f), 500, Easing.OutQuint).OnComplete(x =>
            {
                x.scrollContainer.ScrollbarVisible = true;
                x.scrollContainer.ScrollbarOverlapsContent = false;
            });
        }

        public void Close()
        {
            scrollContainer.ScrollbarVisible = false;
            scrollContainer.ScrollbarOverlapsContent = true;
            this.FadeOut(300, Easing.OutQuint);
            this.ScaleTo(Vector2.Zero, 500, Easing.OutQuint);
            Expire();
        }

        protected override bool OnHover(HoverEvent e)
        {
            return true;
        }

        protected override ITooltip CreateTooltip() => new TextToolTip();

        private class StatScrollContainer : MaxSizeScrollContainer<Drawable>
        {
            public StatScrollContainer(Direction direction, SizeCacheFillFlowContainer<Drawable> itemsFlow)
                : base(direction, itemsFlow)
            {
            }

            protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) =>
                new BasicScrollContainer(direction)
                {
                    ScrollbarOverlapsContent = false
                };
        }

        private class StatLine : CompositeDrawable, IHasTooltip
        {
            public string Tooltip;

            private StatLine(string description)
            {
                RelativeSizeAxes = Axes.X;
                Height = 20;

                AddRangeInternal(new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(200, 200, 200, 255)
                    },
                    new SpriteText()
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(5, 0),

                        Colour = new Color4(20, 20, 20, 255),
                        Font = FontUsage.Default.With(size: 18),
                        Text = description
                    }
                });
            }

            public StatLine(string description, long value)
                : this(description)
            {
                RollingCounter<long> counter;
                AddInternal(counter = new RollingCounter<long>()
                {
                    Position = new Vector2(-5, 0),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Colour = new Color4(20, 20, 20, 255),
                    Font = FontUsage.Default.With(size: 18),
                });

                counter.Current.Value = value;
            }

            public StatLine(string description, string value)
                : this(description)
            {
                AddInternal(new SpriteText()
                {
                    Position = new Vector2(-5, 0),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Colour = new Color4(20, 20, 20, 255),
                    Font = FontUsage.Default.With(size: 18),
                    Text = value
                });
            }

            public LocalisableString TooltipText => Tooltip;
        }

        private class UnitFlowContainer : Container
        {
            private GridContainer statsContainer;

            private static readonly Color4 seperator_color = new Colour4(150, 150, 150, 255);

            private UnitFlowContainer(string heading, string description, int totalCount, long totalValue)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                RollingCounter<long> countRollingCounter;
                RollingCounter<long> valueRollingCounter;
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Colour4(200, 200, 200, 255)
                    },
                    new SpriteText()
                    {
                        Position = new Vector2(5, 0),
                        Font = FontUsage.Default.With(size: 18),
                        Colour = new Color4(20, 20, 20, 255),
                        Text = heading
                    },
                    new FillFlowContainer()
                    {
                        Position = new Vector2(-5, 0),
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Height = 18,
                        AutoSizeAxes = Axes.X,
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            valueRollingCounter = new RollingCounterWithTooltip<long>
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Tooltip = description,
                                Font = FontUsage.Default.With(size: 14),
                                Colour = new Color4(20, 20, 20, 255)
                            },
                            new SpriteText()
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Font = FontUsage.Default.With(size: 18),
                                Text = " / ",
                                Colour = new Color4(20, 20, 20, 255)
                            },
                            countRollingCounter = new RollingCounterWithTooltip<long>
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Tooltip = description,
                                Font = FontUsage.Default.With(size: 18),
                                Colour = new Color4(20, 20, 20, 255)
                            },
                        }
                    },
                    new Box()
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 3,
                        Position = new Vector2(0, 18),
                        Colour = seperator_color
                    },
                    statsContainer = new GridContainer()
                    {
                        RelativeSizeAxes = Axes.X,
                        Position = new Vector2(0, 21),
                        Width = 0.95f,
                        Margin = new MarginPadding { Horizontal = 5 },
                        AutoSizeAxes = Axes.Y
                    }
                };

                countRollingCounter.Current.Value = totalCount;
                valueRollingCounter.Current.Value = totalValue;
            }

            public UnitFlowContainer(string heading, string countryPath, Dictionary<string, (int, long)> units, string desc, int totalCount, long totalValue)
                : this(heading, desc, totalCount, totalValue)
            {
                var content = new Drawable[(units.Count / 3) + 1][];

                var idx = 0;

                foreach (var stat in units)
                {
                    var row = idx / 3;

                    if (content[row] == null)
                        content[row] = new Drawable[3];

                    content[row][idx % 3] = new UnitStats(null, stat.Key, countryPath, stat.Value.Item1, stat.Value.Item2);
                    idx++;
                }
                setContent(content);
            }

            public UnitFlowContainer(string heading, Dictionary<long, PlayerInfo> players, Dictionary<long, Dictionary<string, (int, long)>> units, string desc, int totalCount, long totalValue)
                : this(heading, desc, totalCount, totalValue)
            {
                var rowCount = 0;

                foreach (var playerStats in units)
                {
                    if (playerStats.Value.Count <= 0)
                        continue;

                    rowCount += ((playerStats.Value.Count - 1) / 3) + 2;
                }

                rowCount = Math.Max(0, rowCount - 1);

                var content = new Drawable[rowCount][];

                var rowIdx = 0;

                foreach (var playerStats in units)
                {
                    var countryPath = players[playerStats.Key].Country.Value.UnitPath;

                    var idx = 0;

                    foreach (var stat in playerStats.Value)
                    {
                        if (content[rowIdx] == null)
                            content[rowIdx] = new Drawable[3];

                        content[rowIdx][idx % 3] = new UnitStats(players[playerStats.Key].Username, stat.Key, countryPath, stat.Value.Item1, stat.Value.Item2);
                        idx++;

                        if (idx % 3 == 0)
                            rowIdx++;
                    }

                    if (idx % 3 != 0)
                        rowIdx++;

                    if (rowIdx >= rowCount)
                        continue;

                    content[rowIdx] = new Drawable[3];
                    content[rowIdx][0] = new Box() { RelativeSizeAxes = Axes.X, Height = 2, Colour = seperator_color };
                    content[rowIdx][1] = new Box() { RelativeSizeAxes = Axes.X, Height = 2, Colour = seperator_color };
                    content[rowIdx][2] = new Box() { RelativeSizeAxes = Axes.X, Height = 2, Colour = seperator_color };

                    rowIdx++;
                }
                setContent(content);
            }

            private void setContent(Drawable[][] content)
            {
                statsContainer.Content = content;

                var dimensions = new Dimension[content.Length];
                for (int i = 0; i < content.Length; i++)
                    dimensions[i] = new Dimension(mode: GridSizeMode.AutoSize);

                statsContainer.RowDimensions = dimensions;
            }
        }

        private class UnitStats : FillFlowContainer, IHasTooltip
        {
            private TextureAnimation spriteAnimation;

            private string unitType;
            private string countryPath;
            private long unitValue;
            private string playerUsername;

            public UnitStats(string playerUsername, string unitType, string countryPath, int unitCount, long value)
            {
                AutoSizeAxes = Axes.X;
                Height = 20;

                this.unitType = unitType;
                this.countryPath = countryPath;
                this.playerUsername = playerUsername;
                unitValue = value;

                Spacing = new Vector2(2, 0);

                RollingCounter<long> unitRollingCounter, valueRollingCounter;

                Children = new Drawable[]
                {
                    spriteAnimation = new TextureAnimation()
                    {
                        Size = DrawableTile.BASE_SIZE,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Loop = true
                    },
                    unitRollingCounter = new RollingCounter<long>()
                    {
                        Font = FontUsage.Default.With(size: 18),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RollingDuration = 350,
                        Suffix = " /",
                        Colour = new Color4(20, 20, 20, 255)
                    },
                    valueRollingCounter = new RollingCounter<long>()
                    {
                        Font = FontUsage.Default.With(size: 13),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RollingDuration = 350,
                        Colour = new Color4(20, 20, 20, 255)
                    },
                };

                unitRollingCounter.Current.Value = unitCount;
                valueRollingCounter.Current.Value = unitValue;
            }

            [BackgroundDependencyLoader]
            private void load(NearestNeighbourTextureStore textureStore, UnitStorage unitStorage)
            {
                var unitData = unitStorage.GetUnitByCode(unitType);
                textureStore.LoadIntoAnimation($"{countryPath}/{unitData.IdleAnimation.Texture}", spriteAnimation, unitData.IdleAnimation.Frames, unitData.IdleAnimation.FrameOffset);
            }

            public LocalisableString TooltipText => playerUsername;
        }

        public MenuItem[] ContextMenuItems => createMenuItems();

        private MenuItem[] createMenuItems()
        {
            var playerItems = new List<MenuItem>();

            foreach (var player in players)
            {
                if (player.Key == PlayerID)
                    continue;

                playerItems.Add(new MenuItem(player.Value.Username, () => compareToAction(PlayerID, player.Key)));
            }

            return new MenuItem[]
            {
                new MenuItem("Compare To")
                {
                    Items = playerItems
                }
            };
        }
    }
}
