using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;

namespace AWBWApp.Game.UI.Replay
{
    public class StatsPopup : VisibilityContainer
    {
        /// <summary>
        /// The stats readout that would be shown if the stats window was opened. This can be edited without mutating other turns.
        /// </summary>
        public Dictionary<long, PlayerStatsReadout> CurrentTurnStatsReadout = new Dictionary<long, PlayerStatsReadout>();

        private readonly List<Dictionary<long, PlayerStatsReadout>> registeredReadouts = new List<Dictionary<long, PlayerStatsReadout>>();

        public StatsPopup(Bindable<int> turnNumberBindable)
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
            if (registeredReadouts.Count == 0)
                return;

            var readouts = registeredReadouts[newTurn];

            CurrentTurnStatsReadout.Clear();

            foreach (var readout in readouts)
                CurrentTurnStatsReadout.Add(readout.Key, readout.Value.Clone());
        }

        protected override void PopIn()
        {
        }

        protected override void PopOut()
        {
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
            var undo = (statType ^ UnitStatType.Undo) != 0;

            //Reset the enum to only values we care about
            statType &= UnitStatType.UnitStatsMask;

            (int unitCount, long unitValue) unitStats;

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

            if (!stats.TryGetValue(unitName, out unitStats))
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
