using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Components;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class StatsHandler : Container<StatsPopup>
    {
        /// <summary>
        /// The stats readout that would be shown if the stats window was opened. This can be edited without mutating other turns.
        /// </summary>
        public Dictionary<long, PlayerStatsReadout> CurrentTurnStatsReadout = new Dictionary<long, PlayerStatsReadout>();

        private readonly List<Dictionary<long, PlayerStatsReadout>> registeredReadouts = new List<Dictionary<long, PlayerStatsReadout>>();

        public StatsHandler(Bindable<int> turnNumberBindable)
        {
            turnNumberBindable.BindValueChanged(x => turnChanged(x.NewValue));
        }

        public void RegisterReadouts(Dictionary<long, PlayerStatsReadout> readouts)
        {
            registeredReadouts.Add(readouts);

            if (registeredReadouts.Count == 1)
                turnChanged(0);
        }

        public void ClearReadouts()
        {
            CurrentTurnStatsReadout.Clear();
            registeredReadouts.Clear();
        }

        private void turnChanged(int newTurn)
        {
            if (registeredReadouts.Count == 0 || newTurn < 0)
                return;

            var readouts = registeredReadouts[newTurn];

            CurrentTurnStatsReadout.Clear();

            foreach (var readout in readouts)
                CurrentTurnStatsReadout.Add(readout.Key, readout.Value.Clone());
        }

        public void ShowStatsForPlayer(Dictionary<long, PlayerInfo> players, long playerID)
        {
            foreach (var popup in Children)
                popup.Close();

            Add(new StatsPopup(players, playerID, CurrentTurnStatsReadout[playerID]));
        }
    }

    public class StatsPopup : TooltipContainer
    {
        private StatScrollContainer scrollContainer;

        public StatsPopup(Dictionary<long, PlayerInfo> players, long playerID, PlayerStatsReadout readout)
        {
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
                    new StatLine("Total Generated Funds", readout.GeneratedMoney),
                    new StatLine("Funds Spent Building", readout.MoneySpentOnBuildingUnits),
                    new StatLine("Funds Spent Repairing", readout.MoneySpentOnRepairingUnits),
                    new StatLine("Powers Used", $"{readout.PowersUsed} COP / {readout.SuperPowersUsed} SCOP"),
                    new UnitFlowContainer("Built Units", "Units Built", "Total Value", players[playerID].Country.Value.Code, readout.BuildStats),
                    new UnitFlowContainer("Damaged Units", "Units Killed", "Value of Damage", players[playerID].Country.Value.Code, readout.LostStats),
                    new UnitFlowContainer("Damage to Opponent Units", "Units Killed", "Value of Damage", players, readout.DamageOtherStats),
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
            this.ScaleTo(new Vector2(0.5f, 0f)).ScaleTo(Vector2.One, 500, Easing.OutQuint).OnComplete(x =>
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
            this.ScaleTo(new Vector2(0.5f, 0f), 500, Easing.OutQuint);
            Expire();
        }

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case TouchEvent _:
                case KeyDownEvent _:
                case KeyUpEvent _:
                    return false;
            }

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

        private class StatLine : CompositeDrawable
        {
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
        }

        private class UnitFlowContainer : TooltipContainer
        {
            private GridContainer statsContainer;

            private UnitFlowContainer(string heading)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

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
                    new Box()
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 3,
                        Position = new Vector2(0, 18),
                        Colour = new Colour4(150, 150, 150, 255)
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
            }

            public UnitFlowContainer(string heading, string unitDesc, string valueDesc, string countryCode, Dictionary<string, (int, long)> units)
                : this(heading)
            {
                var content = new Drawable[(units.Count / 3) + 1][];

                var idx = 0;

                foreach (var stat in units)
                {
                    var row = idx / 3;

                    if (content[row] == null)
                        content[row] = new Drawable[3];

                    content[row][idx % 3] = new UnitStats(unitDesc, valueDesc, stat.Key, countryCode, stat.Value.Item1, stat.Value.Item2);
                    idx++;
                }
                setContent(content);
            }

            public UnitFlowContainer(string heading, string unitDesc, string valueDesc, Dictionary<long, PlayerInfo> players, Dictionary<long, Dictionary<string, (int, long)>> units)
                : this(heading)
            {
                var idx = 0;

                var entryCount = 0;

                foreach (var playerStats in units)
                    entryCount += playerStats.Value.Count;

                var content = new Drawable[(entryCount / 3) + 1][];

                foreach (var playerStats in units)
                {
                    var countryCode = players[playerStats.Key].Country.Value.Code;

                    foreach (var stat in playerStats.Value)
                    {
                        var row = idx / 3;

                        if (content[row] == null)
                            content[row] = new Drawable[3];

                        content[row][idx % 3] = new UnitStats(unitDesc, valueDesc, stat.Key, countryCode, stat.Value.Item1, stat.Value.Item2);
                        idx++;
                    }
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

        private class UnitStats : FillFlowContainer
        {
            private TextureAnimation spriteAnimation;

            private string unitType;
            private string countryCode;
            private long unitValue;

            public UnitStats(string unitDesc, string valueDesc, string unitType, string countryCode, int unitCount, long value)
            {
                AutoSizeAxes = Axes.X;
                Height = 20;

                this.unitType = unitType;
                this.countryCode = countryCode;
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
                    unitRollingCounter = new TooltipRollingCounter<long>(unitDesc)
                    {
                        Font = FontUsage.Default.With(size: 18),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RollingDuration = 500,
                        Colour = new Color4(20, 20, 20, 255)
                    },
                    new SpriteText()
                    {
                        Font = FontUsage.Default.With(size: 18),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "/",
                        Colour = new Color4(20, 20, 20, 255)
                    },
                    valueRollingCounter = new TooltipRollingCounter<long>(valueDesc)
                    {
                        Font = FontUsage.Default.With(size: 13),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RollingDuration = 500,
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

                if (unitData.Frames == null)
                {
                    var texture = textureStore.Get($"{unitData.BaseTextureByTeam[countryCode]}-0");
                    spriteAnimation.Size = texture.Size;
                    spriteAnimation.AddFrame(texture);
                    return;
                }

                for (var i = 0; i < unitData.Frames.Length; i++)
                {
                    var texture = textureStore.Get($"{unitData.BaseTextureByTeam[countryCode]}-{i}");
                    if (texture == null)
                        throw new Exception("Improperly configured UnitData. Animation count wrong.");

                    if (i == 0)
                        spriteAnimation.Size = texture.Size;
                    spriteAnimation.AddFrame(texture, unitData.Frames[i]);
                }
            }

            private class TooltipRollingCounter<T> : RollingCounter<T>, IHasTooltip where T : struct, IEquatable<T>
            {
                private string prefix;

                public TooltipRollingCounter(string prefix)
                {
                    this.prefix = prefix;
                }

                public LocalisableString TooltipText => $"{prefix}";
            }
        }
    }

    public class PlayerStatsReadout
    {
        public long GeneratedMoney;
        public long MoneySpentOnBuildingUnits;
        public long MoneySpentOnRepairingUnits;

        public int PowersUsed;
        public int SuperPowersUsed;

        public long TotalValueBuilt;
        public Dictionary<string, (int, long)> BuildStats = new Dictionary<string, (int, long)>();

        public long TotalValueLost;
        public Dictionary<string, (int, long)> LostStats = new Dictionary<string, (int, long)>();

        public long TotalValueDamaged;
        public Dictionary<long, Dictionary<string, (int, long)>> DamageOtherStats = new Dictionary<long, Dictionary<string, (int, long)>>();

        public void RegisterUnitStats(UnitStatType statType, string unitName, long unitOwner, int valueChange)
        {
            var unitLostOrGained = (statType & UnitStatType.UnitCountChanged) != 0;
            var undo = (statType & UnitStatType.Undo) != 0;

            //Reset the enum to only values we care about
            statType &= UnitStatType.UnitStatsMask;

            switch (statType)
            {
                default:
                    throw new ArgumentException("Unknown UnitStatType: " + statType.ToString(), nameof(statType));

                case UnitStatType.BuildUnit:
                {
                    if (!BuildStats.TryGetValue(unitName, out (int unitCount, long unitValue) unitStats))
                        unitStats = (0, 0);

                    if (unitLostOrGained)
                        unitStats.unitCount += undo ? -1 : 1;

                    var change = undo ? -valueChange : valueChange;

                    TotalValueBuilt += change;
                    unitStats.unitValue += change;
                    BuildStats[unitName] = unitStats;
                    break;
                }

                case UnitStatType.LostUnit:
                {
                    if (!LostStats.TryGetValue(unitName, out (int unitCount, long unitValue) unitStats))
                        unitStats = (0, 0);

                    if (unitLostOrGained)
                        unitStats.unitCount += undo ? -1 : 1;

                    var change = undo ? -valueChange : valueChange;

                    TotalValueLost += change;
                    unitStats.unitValue += change;
                    LostStats[unitName] = unitStats;
                    break;
                }

                case UnitStatType.DamageUnit:
                {
                    if (!DamageOtherStats.TryGetValue(unitOwner, out var stats))
                        DamageOtherStats[unitOwner] = stats = new Dictionary<string, (int, long)>();

                    if (!stats.TryGetValue(unitName, out (int unitCount, long unitValue) unitStats))
                        unitStats = (0, 0);

                    if (unitLostOrGained)
                        unitStats.unitCount += undo ? -1 : 1;

                    var change = undo ? -valueChange : valueChange;

                    TotalValueDamaged += change;
                    unitStats.unitValue += change;
                    stats[unitName] = unitStats;
                    break;
                }
            }
        }

        public PlayerStatsReadout Clone()
        {
            var readout = new PlayerStatsReadout();

            readout.GeneratedMoney = GeneratedMoney;

            readout.SuperPowersUsed = SuperPowersUsed;
            readout.PowersUsed = PowersUsed;
            readout.MoneySpentOnBuildingUnits = MoneySpentOnBuildingUnits;
            readout.MoneySpentOnRepairingUnits = MoneySpentOnRepairingUnits;

            readout.TotalValueBuilt = TotalValueBuilt;
            readout.TotalValueLost = TotalValueLost;
            readout.TotalValueDamaged = TotalValueDamaged;

            foreach (var stat in BuildStats)
                readout.BuildStats[stat.Key] = stat.Value;

            foreach (var stat in LostStats)
                readout.LostStats[stat.Key] = stat.Value;

            foreach (var player in DamageOtherStats)
            {
                var stats = new Dictionary<string, (int, long)>();

                foreach (var stat in player.Value)
                    stats[stat.Key] = stat.Value;

                readout.DamageOtherStats[player.Key] = stats;
            }

            return readout;
        }
    }

    [Flags]
    public enum UnitStatType
    {
        None = 0,
        BuildUnit = 1 << 0,
        LostUnit = 1 << 1,
        DamageUnit = 1 << 2,

        UnitStatsMask = BuildUnit | LostUnit | DamageUnit,

        UnitCountChanged = 1 << 3,
        Undo = 1 << 4,
    }
}
