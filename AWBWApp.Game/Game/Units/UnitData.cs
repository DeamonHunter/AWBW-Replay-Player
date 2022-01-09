using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using Newtonsoft.Json;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Units
{
    public class UnitData
    {
        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public int AWBWId { get; set; }

        [JsonProperty]
        public int MaxFuel { get; set; }

        [JsonProperty]
        public int MaxAmmo { get; set; }

        [JsonProperty]
        public int MovementRange { get; set; }

        [JsonProperty]
        public int FuelUsagePerTurn { get; set; }

        [JsonProperty]
        public int Vision { get; set; }

        [JsonProperty]
        public Vector2I AttackRange { get; set; }

        [JsonProperty]
        public int Cost { get; set; }

        [JsonProperty]
        public MovementType MovementType { get; set; }

        [JsonProperty]
        public Dictionary<string, string> BaseTextureByTeam { get; set; }

        [JsonProperty]
        public Dictionary<string, string> DivedTextureByTeam { get; set; }

        [JsonProperty]
        public double[] Frames { get; set; }

        [JsonProperty]
        public double FrameOffset { get; set; }
    }
}
