using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Building
{
    public class BuildingTile
    {
        [JsonProperty]
        public int AWBWID { get; set; }

        [JsonProperty]
        public int CountryID { get; set; }

        [JsonProperty]
        public int BaseDefence { get; set; }

        [JsonProperty]
        public int SightDistanceIncrease { get; set; }

        [JsonProperty]
        public bool GivesMoneyWhenCaptured { get; set; } = true;

        [JsonProperty]
        public int LimitFogOfWarSightDistance { get; set; } = -1;

        [JsonProperty]
        public Dictionary<MovementType, int> MovementCostsPerType { get; set; }

        [JsonProperty]
        public Dictionary<Weather, string> Textures { get; set; }

        [JsonProperty]
        public double[] Frames { get; set; }

        [JsonProperty]
        public double FrameOffset { get; set; }
    }
}
