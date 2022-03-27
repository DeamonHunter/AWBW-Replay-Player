using System.Collections.Generic;
using AWBWApp.Game.Game.Tile;
using Newtonsoft.Json;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.COs
{
    public class COData
    {
        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public int AWBWId { get; set; }

        [JsonProperty]
        public COPower DayToDayPower;

        [JsonProperty]
        public COPower NormalPower;

        [JsonProperty]
        public COPower SuperPower;
    }

    public class COPower
    {
        [JsonProperty]
        public List<UnitPowerIncrease> PowerIncreases;

        [JsonProperty]
        public float UnitPriceMultiplier = 1;

        [JsonProperty]
        public int PropertyFundIncrease;

        [JsonProperty]
        public bool HiddenHP;

        [JsonProperty]
        public bool AttackFirst;

        [JsonProperty]
        public bool SeeIntoHiddenTiles;

        [JsonProperty]
        public int SightIncrease;

        [JsonProperty]
        public int PowerStars;

        [JsonProperty]
        public int AirFuelUsageDecrease;

        [JsonProperty]
        public float CounterAttackPower = 1;

        [JsonProperty]
        public Vector2I? LuckRange;
    }

    public class UnitPowerIncrease
    {
        [JsonProperty]
        public HashSet<string> AffectedUnits;

        [JsonProperty]
        public float PowerIncrease;

        [JsonProperty]
        public float DefenseIncrease;

        [JsonProperty]
        public int RangeIncrease;

        [JsonProperty]
        public TerrainType ConditionalTerrain;
    }
}
