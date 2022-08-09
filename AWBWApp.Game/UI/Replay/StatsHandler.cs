using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.UI.Stats;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace AWBWApp.Game.UI.Replay
{
    public class StatsHandler : Container
    {
        /// <summary>
        /// The stats readout that would be shown if the stats window was opened. This can be edited without mutating other turns.
        /// </summary>
        public Dictionary<long, PlayerStatsReadout> CurrentTurnStatsReadout = new Dictionary<long, PlayerStatsReadout>();

        public List<Dictionary<long, PlayerStatsReadout>> RegisteredReadouts = new List<Dictionary<long, PlayerStatsReadout>>();

        private FillFlowContainer<StatsPopup> fillFlowContainer;
        private DayToDayStatGraph statGraph;
        private int turn;

        public StatsHandler()
        {
            Children = new Drawable[]
            {
                statGraph = new DayToDayStatGraph()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0
                },
                fillFlowContainer = new FillFlowContainer<StatsPopup>()
                {
                    Direction = FillDirection.Horizontal,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    AutoSizeDuration = 15,
                    Spacing = new Vector2(5, 0)
                }
            };
        }

        public void SetStatsToTurn(int turnID)
        {
            if (RegisteredReadouts.Count == 0 || turnID < 0)
                return;

            turn = turnID;
            var readouts = RegisteredReadouts[turnID];

            CurrentTurnStatsReadout.Clear();

            foreach (var readout in readouts)
                CurrentTurnStatsReadout.Add(readout.Key, readout.Value.Clone());
        }

        public void RegisterReadouts(Dictionary<long, PlayerStatsReadout> readouts)
        {
            RegisteredReadouts.Add(readouts);

            if (RegisteredReadouts.Count == 1)
                SetStatsToTurn(0);
        }

        public void ClearReadouts()
        {
            CurrentTurnStatsReadout.Clear();
            RegisteredReadouts.Clear();
        }

        public void ShowStatsForPlayer(Dictionary<long, PlayerInfo> players, long playerID)
        {
            var childCount = fillFlowContainer.Children.Count;

            foreach (var popup in fillFlowContainer.Children)
            {
                if (popup.PlayerID != playerID)
                    continue;

                childCount -= 1;
                popup.Close();
            }

            for (int i = childCount - 2; i >= 0; i--)
                fillFlowContainer.Children[i].Close();

            fillFlowContainer.Add(new StatsPopup(this, players, playerID, turn, (p1, p2) => ComparePlayers(players, p1, p2)));
        }

        public void ComparePlayers(Dictionary<long, PlayerInfo> players, long player1, long player2)
        {
            foreach (var popup in fillFlowContainer.Children)
            {
                if (popup.PlayerID == player1 || popup.PlayerID == player2)
                    continue;

                popup.Close();
            }

            if (!fillFlowContainer.Children.Any(x => x.PlayerID == player1))
                fillFlowContainer.Add(new StatsPopup(this, players, player1, turn, (p1, p2) => ComparePlayers(players, p1, p2)));

            if (!fillFlowContainer.Children.Any(x => x.PlayerID == player2))
                fillFlowContainer.Add(new StatsPopup(this, players, player2, turn, (p1, p2) => ComparePlayers(players, p1, p2)));
        }

        public void CloseAllStats()
        {
            foreach (var popup in fillFlowContainer.Children)
                popup.Close();

            statGraph.Hide();
        }
    }

    public class PlayerStatsReadout
    {
        public long GeneratedMoney;
        public long MoneySpentOnBuildingUnits;
        public long MoneySpentOnRepairingUnits;

        public int PowersUsed;
        public int SuperPowersUsed;

        public int TotalCountBuilt;
        public long TotalValueBuilt;
        public Dictionary<string, (int, long)> BuildStats = new Dictionary<string, (int, long)>();

        public int TotalCountJoin;
        public long TotalValueJoin;
        public Dictionary<string, (int, long)> JoinStats = new Dictionary<string, (int, long)>();

        public int TotalCountLost;
        public long TotalValueLost;
        public Dictionary<string, (int, long)> LostStats = new Dictionary<string, (int, long)>();

        public int TotalCountDamaged;
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
                    {
                        unitStats.unitCount += undo ? -1 : 1;
                        TotalCountBuilt += undo ? -1 : 1;
                    }

                    var change = undo ? -valueChange : valueChange;

                    TotalValueBuilt += change;
                    unitStats.unitValue += change;
                    BuildStats[unitName] = unitStats;
                    break;
                }

                case UnitStatType.JoinUnit:
                {
                    if (!JoinStats.TryGetValue(unitName, out (int unitCount, long unitValue) unitStats))
                        unitStats = (0, 0);

                    if (unitLostOrGained)
                    {
                        unitStats.unitCount += undo ? -1 : 1;
                        TotalCountJoin += undo ? -1 : 1;
                    }

                    var change = undo ? -valueChange : valueChange;

                    TotalValueJoin += change;
                    unitStats.unitValue += change;
                    JoinStats[unitName] = unitStats;
                    break;
                }

                case UnitStatType.LostUnit:
                {
                    if (!LostStats.TryGetValue(unitName, out (int unitCount, long unitValue) unitStats))
                        unitStats = (0, 0);

                    if (unitLostOrGained)
                    {
                        unitStats.unitCount += undo ? -1 : 1;
                        TotalCountLost += undo ? -1 : 1;
                    }

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
                    {
                        unitStats.unitCount += undo ? -1 : 1;
                        TotalCountDamaged += undo ? -1 : 1;
                    }

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
            var readout = new PlayerStatsReadout
            {
                GeneratedMoney = GeneratedMoney,
                SuperPowersUsed = SuperPowersUsed,
                PowersUsed = PowersUsed,
                MoneySpentOnBuildingUnits = MoneySpentOnBuildingUnits,
                MoneySpentOnRepairingUnits = MoneySpentOnRepairingUnits,
                TotalCountBuilt = TotalCountBuilt,
                TotalValueBuilt = TotalValueBuilt,
                TotalCountJoin = TotalCountJoin,
                TotalValueJoin = TotalValueJoin,
                TotalCountLost = TotalCountLost,
                TotalValueLost = TotalValueLost,
                TotalCountDamaged = TotalCountDamaged,
                TotalValueDamaged = TotalValueDamaged
            };

            foreach (var stat in BuildStats)
                readout.BuildStats[stat.Key] = stat.Value;

            foreach (var stat in JoinStats)
                readout.JoinStats[stat.Key] = stat.Value;

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
        JoinUnit = 1 << 3,

        UnitStatsMask = BuildUnit | LostUnit | DamageUnit | JoinUnit,

        UnitCountChanged = 1 << 4,
        Undo = 1 << 5,
    }
}
