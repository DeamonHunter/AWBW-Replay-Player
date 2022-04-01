using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;

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
