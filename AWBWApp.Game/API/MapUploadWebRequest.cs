using System.Net.Http;
using System.Text;
using AWBWApp.Game.API.Replay;
using osu.Framework.IO.Network;
using osu.Framework.Logging;

namespace AWBWApp.Game.API
{
    public class MapUploadWebRequest : WebRequest
    {
        public MapUploadWebRequest(long mapID, ReplayMap mapToUpload)
            : base("https://awbw.amarriner.com/updatemap.php")
        {
            Method = HttpMethod.Post;

            ContentType = "application/x-www-form-urlencoded";

            var output = createMapString(mapID, mapToUpload);
            Logger.Log(output);

            AddRaw(output);
        }

        /// <summary>
        /// Hand craft the string, as WebRequest doesn't allow repeat form parameters
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="mapToUpload"></param>
        /// <returns></returns>
        private string createMapString(long mapId, ReplayMap mapToUpload)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"updatemap=1&maps_id={mapId}");

            for (int x = 0; x < mapToUpload.Size.X; x++)
            {
                for (int y = 0; y < mapToUpload.Size.Y; y++)
                {
                    stringBuilder.Append($"&square%5B%5D={x}%2C{y}%2C{mapToUpload.Ids[y * mapToUpload.Size.X + x]}");

                    //Todo: Add unit placement
                    stringBuilder.Append($"&units_id%5B%5D={x}%2C{y}%2C");
                    stringBuilder.Append($"&code%5B%5D={x}%2C{y}%2C");
                }
            }

            return stringBuilder.ToString();
        }
    }
}
