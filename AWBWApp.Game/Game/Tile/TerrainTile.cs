using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;

namespace AWBWApp.Game.Game.Tile
{
    public class TerrainTile
    {
        public int AWBWId { get; set; }

        public int BaseDefence { get; set; }

        public int SightDistanceIncrease { get; set; }

        public int LimitFogOfWarSightDistance { get; set; } = -1;

        public Dictionary<MovementType, int> MovementCostsPerType { get; set; }

        public Dictionary<Weather, string> Textures { get; set; }

        public string Colour { get; set; }

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
