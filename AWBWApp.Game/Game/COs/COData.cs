using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.COs
{
    public class COData
    {
        public string Name { get; set; }
        public int AWBWId { get; set; }
        public string Tooltip;

        public COPower DayToDayPower;
        public COPower NormalPower;
        public COPower SuperPower;
    }

    public class COPower
    {
        public List<UnitPowerIncrease> PowerIncreases;
        public float UnitPriceMultiplier = 1;
        public int PropertyFundIncrease;
        public bool HiddenHP;
        public bool AttackFirst;
        public bool SeeIntoHiddenTiles;
        public int SightIncrease;
        public int PowerStars;
        public int AirFuelUsageDecrease;
        public float CounterAttackPower = 1;
        public Vector2I? LuckRange;

        public int? MoveCostPerTile;
        public Weather WeatherWithNoMovementAffect;
        public Weather WeatherWithAdditionalMovementAffect;
    }

    public class UnitPowerIncrease
    {
        public HashSet<string> AffectedUnits;
        public float PowerIncrease;
        public float DefenseIncrease;
        public int RangeIncrease;
        public TerrainType ConditionalTerrain;
    }
}
