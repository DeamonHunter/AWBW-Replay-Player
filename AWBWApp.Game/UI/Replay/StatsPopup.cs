using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Components.Tooltip;
using AWBWApp.Game.UI.Stats;
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
        private DayToDayStatGraph statGraph;

        private Dictionary<long, PlayerInfo> players;
        private StatsHandler statsHandler;
        private int turnNumber;
        private int graphDepth;

        private Action<long, long> compareToAction;

        public StatsPopup(StatsHandler statsHandler, Dictionary<long, PlayerInfo> players, long playerID, int turnNumber, Action<long, long> compareToAction)
        {
            PlayerID = playerID;
            this.players = players;
            this.compareToAction = compareToAction;
            this.statsHandler = statsHandler;
            this.turnNumber = turnNumber;

            AutoSizeAxes = Axes.Y;

            Width = 300;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            Masking = true;
            CornerRadius = 8;

            var readout = statsHandler.CurrentTurnStatsReadout[PlayerID];

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
                        Tooltip = $"Spent on Building: {readout.MoneySpentOnBuildingUnits}\nSpent On Repairing: {readout.MoneySpentOnRepairingUnits}",
                        OnClickAction = () => showGraphForStat(Stat.GeneratedMoney, 1)
                    },
                    new StatLine("Powers Used", $"{readout.PowersUsed} COP / {readout.SuperPowersUsed} SCOP")
                    {
                        OnClickAction = () => showGraphForStat(Stat.PowersUsed, 2)
                    },
                    new UnitFlowContainer("Built/Value", players[playerID].Country.Value.UnitPath, readout.BuildStats, "Built Units/Built Unit Value", readout.TotalCountBuilt, readout.TotalValueBuilt)
                    {
                        OnClickAction = () => showGraphForStat(Stat.BuildValue, 3)
                    },
                    new UnitFlowContainer("Joined Units/Funds Gained", players[playerID].Country.Value.UnitPath, readout.JoinStats, "Joined Units/Funds Gained", readout.TotalCountJoin, readout.TotalValueJoin)
                    {
                        OnClickAction = () => showGraphForStat(Stat.JoinValue, 4)
                    },
                    new UnitFlowContainer("Deaths/Value Damage Taken", players[playerID].Country.Value.UnitPath, readout.LostStats, "Lost Units/Unit Value Lost", readout.TotalCountLost, readout.TotalValueLost)
                    {
                        OnClickAction = () => showGraphForStat(Stat.LostValue, 5)
                    },
                    new UnitFlowContainer("Kills/Value Damage Dealt", players, readout.DamageOtherStats, "Killed Units/Unit Value Damage", readout.TotalCountDamaged, readout.TotalValueDamaged)
                    {
                        OnClickAction = () => showGraphForStat(Stat.DestroyedValue, 6)
                    },
                    statGraph = new DayToDayStatGraph()
                    {
                        RelativeSizeAxes = Axes.X,
                        Alpha = 0,
                        Margin = new MarginPadding { Top = -5 }
                    }
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

        //Todo: This function is very ugly with lots of repeated stuff.
        private void showGraphForStat(Stat stat, int depth)
        {
            if (statGraph.Alpha > 0 && graphDepth == depth)
            {
                statGraph.Hide();
                return;
            }

            graphDepth = depth;
            scrollContainer.ReorderChild(statGraph, depth - 0.5f);
            statGraph.Show();
            statGraph.ClearPaths();

            //Todo: Smooth popout doesn't look good due to invalidation issues with grid container
            //statGraph.ScaleTo(new Vector2(1f, 0.05f)).ScaleTo(1, 150, Easing.OutQuint);

            statGraph.PlayerCount = 1;
            var otherPlayer = players.First(x => x.Key != PlayerID);

            switch (stat)
            {
                case Stat.GeneratedMoney:
                {
                    var statForCurrentPlayer = new List<float>();
                    var diffForCurrentPlayer = new List<float>();
                    var statComparison = new List<float>();
                    var diffComparison = new List<float>();

                    float playerPrevious = 0;
                    float comparisonPrevious = 0;

                    for (int i = 0; i < turnNumber; i += players.Count)
                    {
                        var playerCurrent = statsHandler.RegisteredReadouts[i][PlayerID].GeneratedMoney;
                        statForCurrentPlayer.Add(playerCurrent);
                        diffForCurrentPlayer.Add(playerCurrent - playerPrevious);
                        playerPrevious = playerCurrent;

                        if (players.Count > 2)
                        {
                            var average = 0f;
                            foreach (var player in players)
                                average += statsHandler.RegisteredReadouts[i][player.Key].GeneratedMoney;

                            average /= players.Count;

                            statComparison.Add(average);
                            diffComparison.Add(average - comparisonPrevious);
                            comparisonPrevious = average;
                        }
                        else
                        {
                            var comparisonCurrent = statsHandler.RegisteredReadouts[i][otherPlayer.Key].GeneratedMoney;
                            statComparison.Add(comparisonCurrent);
                            diffComparison.Add(comparisonCurrent - comparisonPrevious);
                            comparisonPrevious = comparisonCurrent;
                        }
                    }

                    var lastPoint = statsHandler.CurrentTurnStatsReadout[PlayerID].GeneratedMoney;
                    statForCurrentPlayer.Add(lastPoint);
                    diffForCurrentPlayer.Add(lastPoint - playerPrevious);

                    if (players.Count > 2)
                    {
                        var average = 0f;
                        foreach (var player in players)
                            average += statsHandler.CurrentTurnStatsReadout[player.Key].GeneratedMoney;

                        average /= players.Count;

                        statComparison.Add(average);
                        diffComparison.Add(average - comparisonPrevious);
                    }
                    else
                    {
                        var comparisonCurrent = statsHandler.CurrentTurnStatsReadout[otherPlayer.Key].GeneratedMoney;
                        statComparison.Add(comparisonCurrent);
                        diffComparison.Add(comparisonCurrent - comparisonPrevious);
                    }

                    statGraph.AddPath("All Time", Color4.Red, statForCurrentPlayer);
                    statGraph.AddPath(players.Count > 2 ? "Average All Time" : "Opponent All Time", Color4.Blue, statComparison);
                    statGraph.AddPath("Per Turn", Color4.Purple, diffForCurrentPlayer);
                    statGraph.AddPath(players.Count > 2 ? "Average Per Turn" : "Opponent Per Turn", Color4.Green, diffComparison);
                    break;
                }

                case Stat.PowersUsed:
                {
                    var powersUsed = new List<float>();
                    var superPowersUsed = new List<float>();
                    var comparisonPowersUsed = new List<float>();
                    var comparisonSuperPowersUsed = new List<float>();

                    for (int i = 0; i < turnNumber; i += players.Count)
                    {
                        powersUsed.Add(statsHandler.RegisteredReadouts[i][PlayerID].PowersUsed);
                        superPowersUsed.Add(statsHandler.RegisteredReadouts[i][PlayerID].SuperPowersUsed);

                        if (players.Count > 2)
                        {
                            var averagePower = 0f;
                            var averageSuperPower = 0f;

                            foreach (var player in players)
                            {
                                averagePower += statsHandler.RegisteredReadouts[i][player.Key].PowersUsed;
                                averageSuperPower += statsHandler.RegisteredReadouts[i][player.Key].SuperPowersUsed;
                            }

                            comparisonPowersUsed.Add(averagePower / players.Count);
                            comparisonSuperPowersUsed.Add(averageSuperPower / players.Count);
                        }
                        else
                        {
                            comparisonPowersUsed.Add(statsHandler.RegisteredReadouts[i][otherPlayer.Key].PowersUsed);
                            comparisonSuperPowersUsed.Add(statsHandler.RegisteredReadouts[i][otherPlayer.Key].SuperPowersUsed);
                        }
                    }

                    powersUsed.Add(statsHandler.CurrentTurnStatsReadout[PlayerID].PowersUsed);
                    superPowersUsed.Add(statsHandler.CurrentTurnStatsReadout[PlayerID].SuperPowersUsed);

                    if (players.Count > 2)
                    {
                        var averagePower = 0f;
                        var averageSuperPower = 0f;

                        foreach (var player in players)
                        {
                            averagePower += statsHandler.CurrentTurnStatsReadout[player.Key].PowersUsed;
                            averageSuperPower += statsHandler.CurrentTurnStatsReadout[player.Key].SuperPowersUsed;
                        }

                        comparisonPowersUsed.Add(averagePower / players.Count);
                        comparisonSuperPowersUsed.Add(averageSuperPower / players.Count);
                    }
                    else
                    {
                        comparisonPowersUsed.Add(statsHandler.CurrentTurnStatsReadout[otherPlayer.Key].PowersUsed);
                        comparisonSuperPowersUsed.Add(statsHandler.CurrentTurnStatsReadout[otherPlayer.Key].SuperPowersUsed);
                    }

                    statGraph.AddPath("Supers Used", Color4.Red, superPowersUsed);
                    statGraph.AddPath(players.Count > 2 ? "Average Supers Used" : "Opponent Supers Used", Color4.Blue, comparisonSuperPowersUsed);
                    statGraph.AddPath("Powers Used", Color4.Purple, powersUsed);
                    statGraph.AddPath(players.Count > 2 ? "Average Powers Used" : "Opponent Powers Used", Color4.Green, comparisonPowersUsed);
                    break;
                }

                case Stat.BuildValue:
                {
                    var buildValue = new List<float>();
                    var buildCount = new List<float>();
                    var comparisonBuildValue = new List<float>();
                    var comparisonBuildCount = new List<float>();

                    for (int i = 0; i < turnNumber; i += players.Count)
                    {
                        var valueCount = countValue(statsHandler.RegisteredReadouts[i][PlayerID].BuildStats);
                        buildCount.Add(valueCount.Item1);
                        buildValue.Add(valueCount.Item2);

                        if (players.Count > 2)
                        {
                            var averageCount = 0f;
                            var averageValue = 0f;

                            foreach (var player in players)
                            {
                                valueCount = countValue(statsHandler.RegisteredReadouts[i][player.Key].BuildStats);
                                averageCount += valueCount.Item1;
                                averageValue += valueCount.Item2;
                            }

                            comparisonBuildCount.Add(averageCount / players.Count);
                            comparisonBuildValue.Add(averageValue / players.Count);
                        }
                        else
                        {
                            valueCount = countValue(statsHandler.RegisteredReadouts[i][otherPlayer.Key].BuildStats);
                            comparisonBuildCount.Add(valueCount.Item1);
                            comparisonBuildValue.Add(valueCount.Item2);
                        }
                    }

                    var finalValueCount = countValue(statsHandler.CurrentTurnStatsReadout[PlayerID].BuildStats);
                    buildCount.Add(finalValueCount.Item1);
                    buildValue.Add(finalValueCount.Item2);

                    if (players.Count > 2)
                    {
                        var averageCount = 0f;
                        var averageValue = 0f;

                        foreach (var player in players)
                        {
                            var valueCount = countValue(statsHandler.CurrentTurnStatsReadout[player.Key].BuildStats);
                            averageCount += valueCount.Item1;
                            averageValue += valueCount.Item2;
                        }

                        comparisonBuildCount.Add(averageCount / players.Count);
                        comparisonBuildValue.Add(averageValue / players.Count);
                    }
                    else
                    {
                        var valueCount = countValue(statsHandler.CurrentTurnStatsReadout[otherPlayer.Key].BuildStats);
                        comparisonBuildCount.Add(valueCount.Item1);
                        comparisonBuildValue.Add(valueCount.Item2);
                    }

                    statGraph.AddPath("Total Value", Color4.Red, buildValue);
                    statGraph.AddPath(players.Count > 2 ? "Average Total Value" : "Opponent Total Value", Color4.Blue, comparisonBuildValue);
                    statGraph.AddPath("Total Count", Color4.Purple, buildCount);
                    statGraph.AddPath(players.Count > 2 ? "Average Total Count" : "Opponent Total Count", Color4.Green, comparisonBuildCount);
                    break;
                }

                case Stat.JoinValue:
                {
                    var joinValue = new List<float>();
                    var joinCount = new List<float>();
                    var comparisonJoinValue = new List<float>();
                    var comparisonJoinCount = new List<float>();

                    for (int i = 0; i < turnNumber; i += players.Count)
                    {
                        var valueCount = countValue(statsHandler.RegisteredReadouts[i][PlayerID].JoinStats);
                        joinCount.Add(valueCount.Item1);
                        joinValue.Add(valueCount.Item2);

                        if (players.Count > 2)
                        {
                            var averageCount = 0f;
                            var averageValue = 0f;

                            foreach (var player in players)
                            {
                                valueCount = countValue(statsHandler.RegisteredReadouts[i][player.Key].JoinStats);
                                averageCount += valueCount.Item1;
                                averageValue += valueCount.Item2;
                            }

                            comparisonJoinCount.Add(averageCount / players.Count);
                            comparisonJoinValue.Add(averageValue / players.Count);
                        }
                        else
                        {
                            valueCount = countValue(statsHandler.RegisteredReadouts[i][otherPlayer.Key].JoinStats);
                            comparisonJoinCount.Add(valueCount.Item1);
                            comparisonJoinValue.Add(valueCount.Item2);
                        }
                    }

                    var finalValueCount = countValue(statsHandler.CurrentTurnStatsReadout[PlayerID].JoinStats);
                    joinCount.Add(finalValueCount.Item1);
                    joinValue.Add(finalValueCount.Item2);

                    if (players.Count > 2)
                    {
                        var averageCount = 0f;
                        var averageValue = 0f;

                        foreach (var player in players)
                        {
                            var valueCount = countValue(statsHandler.CurrentTurnStatsReadout[player.Key].JoinStats);
                            averageCount += valueCount.Item1;
                            averageValue += valueCount.Item2;
                        }

                        comparisonJoinCount.Add(averageCount / players.Count);
                        comparisonJoinValue.Add(averageValue / players.Count);
                    }
                    else
                    {
                        var valueCount = countValue(statsHandler.CurrentTurnStatsReadout[otherPlayer.Key].JoinStats);
                        comparisonJoinCount.Add(valueCount.Item1);
                        comparisonJoinValue.Add(valueCount.Item2);
                    }

                    statGraph.AddPath("Total Value", Color4.Red, joinValue);
                    statGraph.AddPath(players.Count > 2 ? "Average Total Value" : "Opponent Total Value", Color4.Blue, comparisonJoinValue);
                    statGraph.AddPath("Total Count", Color4.Purple, joinCount);
                    statGraph.AddPath(players.Count > 2 ? "Average Total Count" : "Opponent Total Count", Color4.Green, comparisonJoinCount);
                    break;
                }

                case Stat.LostValue:
                {
                    var lostValue = new List<float>();
                    var lostCount = new List<float>();
                    var comparisonLostValue = new List<float>();
                    var comparisonLostCount = new List<float>();

                    for (int i = 0; i < turnNumber; i += players.Count)
                    {
                        var valueCount = countValue(statsHandler.RegisteredReadouts[i][PlayerID].LostStats);
                        lostCount.Add(valueCount.Item1);
                        lostValue.Add(valueCount.Item2);

                        if (players.Count > 2)
                        {
                            var averageCount = 0f;
                            var averageValue = 0f;

                            foreach (var player in players)
                            {
                                valueCount = countValue(statsHandler.RegisteredReadouts[i][player.Key].LostStats);
                                averageCount += valueCount.Item1;
                                averageValue += valueCount.Item2;
                            }

                            comparisonLostCount.Add(averageCount / players.Count);
                            comparisonLostValue.Add(averageValue / players.Count);
                        }
                        else
                        {
                            valueCount = countValue(statsHandler.RegisteredReadouts[i][otherPlayer.Key].LostStats);
                            comparisonLostCount.Add(valueCount.Item1);
                            comparisonLostValue.Add(valueCount.Item2);
                        }
                    }

                    var finalValueCount = countValue(statsHandler.CurrentTurnStatsReadout[PlayerID].LostStats);
                    lostCount.Add(finalValueCount.Item1);
                    lostValue.Add(finalValueCount.Item2);

                    if (players.Count > 2)
                    {
                        var averageCount = 0f;
                        var averageValue = 0f;

                        foreach (var player in players)
                        {
                            var valueCount = countValue(statsHandler.CurrentTurnStatsReadout[player.Key].LostStats);
                            averageCount += valueCount.Item1;
                            averageValue += valueCount.Item2;
                        }

                        comparisonLostCount.Add(averageCount / players.Count);
                        comparisonLostValue.Add(averageValue / players.Count);
                    }
                    else
                    {
                        var valueCount = countValue(statsHandler.CurrentTurnStatsReadout[otherPlayer.Key].LostStats);
                        comparisonLostCount.Add(valueCount.Item1);
                        comparisonLostValue.Add(valueCount.Item2);
                    }

                    statGraph.AddPath("Total Value", Color4.Red, lostValue);
                    statGraph.AddPath(players.Count > 2 ? "Average Total Value" : "Opponent Total Value", Color4.Blue, comparisonLostValue);
                    statGraph.AddPath("Total Count", Color4.Purple, lostCount);
                    statGraph.AddPath(players.Count > 2 ? "Average Total Count" : "Opponent Total Count", Color4.Green, comparisonLostCount);
                    break;
                }

                case Stat.DestroyedValue:
                {
                    var destroyedValue = new List<float>();
                    var destroyedCount = new List<float>();
                    var comparisonDestroyedValue = new List<float>();
                    var comparisonDestroyedCount = new List<float>();

                    for (int i = 0; i < turnNumber; i += players.Count)
                    {
                        var valueCount = countAllValue(statsHandler.RegisteredReadouts[i][PlayerID].DamageOtherStats);
                        destroyedCount.Add(valueCount.Item1);
                        destroyedValue.Add(valueCount.Item2);

                        if (players.Count > 2)
                        {
                            var averageCount = 0f;
                            var averageValue = 0f;

                            foreach (var player in players)
                            {
                                valueCount = countAllValue(statsHandler.RegisteredReadouts[i][player.Key].DamageOtherStats);
                                averageCount += valueCount.Item1;
                                averageValue += valueCount.Item2;
                            }

                            comparisonDestroyedCount.Add(averageCount / players.Count);
                            comparisonDestroyedValue.Add(averageValue / players.Count);
                        }
                        else
                        {
                            valueCount = countAllValue(statsHandler.RegisteredReadouts[i][otherPlayer.Key].DamageOtherStats);
                            comparisonDestroyedCount.Add(valueCount.Item1);
                            comparisonDestroyedValue.Add(valueCount.Item2);
                        }
                    }

                    var finalValueCount = countAllValue(statsHandler.CurrentTurnStatsReadout[PlayerID].DamageOtherStats);
                    destroyedCount.Add(finalValueCount.Item1);
                    destroyedValue.Add(finalValueCount.Item2);

                    if (players.Count > 2)
                    {
                        var averageCount = 0f;
                        var averageValue = 0f;

                        foreach (var player in players)
                        {
                            var valueCount = countAllValue(statsHandler.CurrentTurnStatsReadout[player.Key].DamageOtherStats);
                            averageCount += valueCount.Item1;
                            averageValue += valueCount.Item2;
                        }

                        comparisonDestroyedCount.Add(averageCount / players.Count);
                        comparisonDestroyedValue.Add(averageValue / players.Count);
                    }
                    else
                    {
                        var valueCount = countAllValue(statsHandler.CurrentTurnStatsReadout[otherPlayer.Key].DamageOtherStats);
                        comparisonDestroyedCount.Add(valueCount.Item1);
                        comparisonDestroyedValue.Add(valueCount.Item2);
                    }

                    statGraph.AddPath("Total Value", Color4.Red, destroyedValue);
                    statGraph.AddPath(players.Count > 2 ? "Average Total Value" : "Opponent Total Value", Color4.Blue, comparisonDestroyedValue);
                    statGraph.AddPath("Total Count", Color4.Purple, destroyedCount);
                    statGraph.AddPath(players.Count > 2 ? "Average Total Count" : "Opponent Total Count", Color4.Green, comparisonDestroyedCount);
                    break;
                }
            }
        }

        private (int, long) countAllValue(Dictionary<long, Dictionary<string, (int, long)>> playersDict)
        {
            var totalCount = 0;
            var totalValue = 0L;

            foreach (var valueDict in playersDict)
            {
                foreach (var valueEntry in valueDict.Value)
                {
                    totalCount += valueEntry.Value.Item1;
                    totalValue += valueEntry.Value.Item2;
                }
            }

            return (totalCount, totalValue);
        }

        private (int, long) countValue(Dictionary<string, (int, long)> valueDict)
        {
            var totalCount = 0;
            var totalValue = 0L;

            foreach (var valueEntry in valueDict)
            {
                totalCount += valueEntry.Value.Item1;
                totalValue += valueEntry.Value.Item2;
            }

            return (totalCount, totalValue);
        }

        private enum Stat
        {
            GeneratedMoney,
            PowersUsed,
            BuildValue,
            JoinValue,
            LostValue,
            DestroyedValue
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
                    ScrollbarOverlapsContent = false,
                };
        }

        private class StatLine : CompositeDrawable, IHasTooltip
        {
            public string Tooltip;
            public Action OnClickAction;

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

            protected override bool OnClick(ClickEvent e)
            {
                if (OnClickAction != null)
                {
                    OnClickAction.Invoke();
                    return true;
                }

                return base.OnClick(e);
            }
        }

        private class UnitFlowContainer : Container
        {
            private GridContainer statsContainer;
            public Action OnClickAction;

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

            protected override bool OnClick(ClickEvent e)
            {
                if (OnClickAction != null)
                {
                    OnClickAction.Invoke();
                    return true;
                }

                return base.OnClick(e);
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
