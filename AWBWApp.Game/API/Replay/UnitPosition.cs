using Newtonsoft.Json;

namespace AWBWApp.Game.API.Replay
{
    public class UnitPosition
    {
        [JsonProperty]
        public bool Unit_Visible { get; set; }

        [JsonProperty]
        public int X { get; set; }

        [JsonProperty]
        public int Y { get; set; }
    }
}
