using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Tile
{
    public class TerrainTile
    {
        [JsonProperty]
        public int AWBWId { get; set; }

        [JsonProperty]
        public int BaseDefence { get; set; }

        [JsonProperty]
        public int SightDistanceIncrease { get; set; }

        [JsonProperty]
        public int LimitFogOfWarSightDistance { get; set; } = -1;

        [JsonProperty]
        public Dictionary<MovementType, int> MovementCostsPerType { get; set; }

        [JsonProperty]
        public Dictionary<Weather, string> Textures { get; set; }

        [JsonProperty]
        public TerrainType TerrainType { get; set; }
    }

    [Flags]
    public enum TerrainType
    {
        None = 0,
        Plain = 1 << 0,
        Road = 1 << 1,
        Mountain = 1 << 2,
        Forest = 1 << 3,
        River = 1 << 4,
        Building = 1 << 5,
        Sea = 1 << 6,
        Shoal = 1 << 7,
        Land = Plain | Road | Building | Mountain | Forest | River
    }
}
