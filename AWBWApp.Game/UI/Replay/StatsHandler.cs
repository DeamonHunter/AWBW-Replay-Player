using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.Logic;
using osu.Framework.Bindables;
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

        private readonly List<Dictionary<long, PlayerStatsReadout>> registeredReadouts = new List<Dictionary<long, PlayerStatsReadout>>();

        private FillFlowContainer<StatsPopup> fillFlowContainer;

        public StatsHandler(Bindable<int> turnNumberBindable)
        {
            turnNumberBindable.BindValueChanged(x => turnChanged(x.NewValue));

            Child = fillFlowContainer = new FillFlowContainer<StatsPopup>()
            {
                Direction = FillDirection.Horizontal,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                AutoSizeDuration = 15,
                Spacing = new Vector2(5, 0)
            };
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

            fillFlowContainer.Add(new StatsPopup(players, playerID, CurrentTurnStatsReadout[playerID], (p1, p2) => ComparePlayers(players, p1, p2)));
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
                fillFlowContainer.Add(new StatsPopup(players, player1, CurrentTurnStatsReadout[player1], (p1, p2) => ComparePlayers(players, p1, p2)));

            if (!fillFlowContainer.Children.Any(x => x.PlayerID == player2))
                fillFlowContainer.Add(new StatsPopup(players, player2, CurrentTurnStatsReadout[player2], (p1, p2) => ComparePlayers(players, p1, p2)));
        }

        public void CloseAllStats()
        {
            foreach (var popup in fillFlowContainer.Children)
                popup.Close();
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
