using Newtonsoft.Json;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayMap
    {
        [JsonProperty]
        public string TerrainName;
        [JsonProperty]
        public Vector2I Size;
        [JsonProperty]
        public short[] Ids;
    }
}
