using System;
using AWBWApp.Game.API.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API
{
    public class MapDownloadWebRequest : GenericJsonWebRequest
    {
        public ReplayMap ParsedMap;

        public MapDownloadWebRequest(long mapID)
            : base($"https://awbw.amarriner.com/matsuzen/api/map/map_info.php?maps_id={mapID}")
        {
        }

        protected override void ProcessResponse()
        {
            base.ProcessResponse();

            if (ResponseObject == null)
                return;

            ParsedMap = new ReplayMap
            {
                TerrainName = (string)ResponseObject["Name"],
                Size = new Vector2I((int)ResponseObject["Size X"], (int)ResponseObject["Size Y"])
            };

            ParsedMap.Ids = new short[ParsedMap.Size.X * ParsedMap.Size.Y];

            var mapArray = (JArray)ResponseObject["Terrain Map"];
            if (mapArray == null)
                throw new Exception("Malformed map data. Missing map array.");

            for (int x = 0; x < ParsedMap.Size.X; x++)
            {
                var column = mapArray[x];

                for (int y = 0; y < ParsedMap.Size.Y; y++)
                {
                    var value = column[y];
                    if (value == null || value.Type == JTokenType.Null || value.Type == JTokenType.String)
                        ParsedMap.Ids[y * ParsedMap.Size.X + x] = 0;
                    else
                        ParsedMap.Ids[y * ParsedMap.Size.X + x] = (short)value;
                }
            }
        }
    }
}
