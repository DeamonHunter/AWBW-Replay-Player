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
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class StatsHandler : Container
    {
        /// <summary>
        /// The stats readout that would be shown if the stats window was opened. This can be edited without mutating other turns.
        /// </summary>
        public Dictionary<long, PlayerStatsReadout> CurrentTurnStatsReadout = new Dictionary<long, PlayerStatsReadout>();

        private readonly List<Dictionary<long, PlayerStatsReadout>> registeredReadouts = new List<Dictionary<long, PlayerStatsReadout>>();

        public StatsHandler(Bindable<int> turnNumberBindable)
        {
            turnNumberBindable.BindValueChanged(x => turnChanged(x.NewValue));
            RelativeSizeAxes = Axes.Both;
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

        public void ShowStatsForPlayer(PlayerInfo player)
        {
            this.FadeInFromZero(300, Easing.OutQuint);

            Add(new StatsPopup(player, CurrentTurnStatsReadout[player.ID]));
        }
    }

    public class StatsPopup : Container
    {
        public StatsPopup(PlayerInfo player, PlayerStatsReadout readout)
        {
            AutoSizeAxes = Axes.Y;

            Width = 300;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            Masking = true;
            CornerRadius = 8;

            var headerColor = Color4Extensions.FromHex(player.Country.Value.Colours["playerList"]).Darken(0.1f);
            var borderColor = headerColor.Darken(0.2f);

            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both
                },
                new DrawablePlayerListItem(player, false, headerColor, borderColor, Color4.White)
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
                    Action = () =>
                    {
                        this.FadeOut(300, Easing.OutQuint).Expire();
                    }
                },
                new FillFlowContainer()
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Margin = new MarginPadding { Top = 45 },
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 2),
                    Children = new Drawable[]
                    {
                        new SpriteText()
                        {
                            Colour = new Color4(20, 20, 20, 255),
                            Margin = new MarginPadding { Horizontal = 5 },
                            Font = FontUsage.Default.With(size: 18),
                            Text = $"Total Generated Funds: {readout.GeneratedMoney}"
                        },
                        new SpriteText()
                        {
                            Colour = new Color4(20, 20, 20, 255),
                            Margin = new MarginPadding { Horizontal = 5 },
                            Font = FontUsage.Default.With(size: 18),
                            Text = $"Funds Spent Building: {readout.MoneySpentOnBuildingUnits}"
                        },
                        new SpriteText()
                        {
                            Colour = new Color4(20, 20, 20, 255),
                            Margin = new MarginPadding { Horizontal = 5 },
                            Font = FontUsage.Default.With(size: 18),
                            Text = $"Funds Spent Repairing: {readout.MoneySpentOnRepairingUnits}"
                        },
                        new UnitFlowContainer("Units Built", player.Country.Value.Code, readout.BuildStats),
                        new UnitFlowContainer("Units Lost", player.Country.Value.Code, readout.LostStats),
                        new UnitFlowContainer("Opponent Units Destroyed", player.Country.Value.Code, readout.DamageOtherStats),
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            this.FadeInFromZero(300, Easing.OutQuint);
        }

        private class UnitFlowContainer : Container
        {
            public UnitFlowContainer(string heading, string countryCode, Dictionary<string, (int, long)> units)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                FillFlowContainer statsFlowContainer;
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Colour4(200, 200, 200, 255)
                    },
                    new SpriteText()
                    {
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
                    statsFlowContainer = new FillFlowContainer()
                    {
                        RelativeSizeAxes = Axes.X,
                        Position = new Vector2(0, 21),
                        Margin = new MarginPadding { Horizontal = 5 },
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(6, 2),
                    }
                };

                foreach (var stat in units)
                    statsFlowContainer.Add(new UnitStats(stat.Key, countryCode, stat.Value.Item1, stat.Value.Item2));
            }
        }

        private class UnitStats : FillFlowContainer
        {
            private TextureAnimation spriteAnimation;

            private string unitType;
            private string countryCode;

            public UnitStats(string unitType, string countryCode, int unitCount, long value)
            {
                AutoSizeAxes = Axes.X;
                Height = 20;

                this.unitType = unitType;
                this.countryCode = countryCode;

                Spacing = new Vector2(1, 0);

                Children = new Drawable[]
                {
                    spriteAnimation = new TextureAnimation()
                    {
                        Size = DrawableTile.BASE_SIZE,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Loop = true
                    },
                    new SpriteText()
                    {
                        Font = FontUsage.Default.With(size: 18),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Colour = new Color4(20, 20, 20, 255),
                        Text = $"x {unitCount}"
                    },
                    new SpriteText()
                    {
                        Font = FontUsage.Default.With(size: 12),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Padding = new MarginPadding { Bottom = 4 },
                        Colour = new Color4(20, 20, 20, 255),
                        Text = $"({value})"
                    }
                };
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
        public Dictionary<string, (int, long)> DamageOtherStats = new Dictionary<string, (int, long)>();

        public void RegisterUnitStats(UnitStatType statType, string unitName, int valueChange)
        {
            var unitLostOrGained = (statType & UnitStatType.UnitCountChanged) != 0;
            var undo = (statType & UnitStatType.Undo) != 0;

            //Reset the enum to only values we care about
            statType &= UnitStatType.UnitStatsMask;

            Dictionary<string, (int, long)> stats;

            switch (statType)
            {
                default:
                    throw new ArgumentException("Unknown UnitStatType: " + statType.ToString(), nameof(statType));

                case UnitStatType.BuildUnit:
                    stats = BuildStats;
                    TotalValueBuilt += undo ? -valueChange : valueChange;
                    break;

                case UnitStatType.LostUnit:
                    stats = LostStats;
                    TotalValueLost += undo ? -valueChange : valueChange;
                    break;

                case UnitStatType.DamageUnit:
                    stats = DamageOtherStats;
                    TotalValueDamaged += undo ? -valueChange : valueChange;
                    break;
            }

            if (!stats.TryGetValue(unitName, out (int unitCount, long unitValue) unitStats))
                unitStats = (0, 0);

            if (unitLostOrGained)
                unitStats.unitCount += undo ? -1 : 1;

            unitStats.unitValue += undo ? -valueChange : valueChange;
            stats[unitName] = unitStats;
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

            readout.BuildStats = new Dictionary<string, (int, long)>(BuildStats);
            readout.DamageOtherStats = new Dictionary<string, (int, long)>(DamageOtherStats);
            readout.LostStats = new Dictionary<string, (int, long)>(LostStats);

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
