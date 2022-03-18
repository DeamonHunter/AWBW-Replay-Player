using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.IO.Network;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace AWBWApp.Game.IO
{
    public class MapFileStorage : IResourceStore<ReplayMap>
    {
        private const string terrain_folder = "ReplayData/Terrain";

        private readonly Storage underlyingStorage;

        public MapFileStorage(Storage storage)
        {
            underlyingStorage = new WrappedStorage(storage, terrain_folder);
        }

        public ReplayMap Get(string name)
        {
            using (var stream = underlyingStorage.GetStream(name))
            {
                if (stream == null)
                    return null;

                using (StreamReader sr = new StreamReader(stream))
                    return JsonConvert.DeserializeObject<ReplayMap>(sr.ReadToEnd());
            }
        }

        public ReplayMap Get(long mapID) => Get($"{mapID}.json");

        public async Task<ReplayMap> GetOrDownloadMap(long mapID)
        {
            var map = Get(mapID);

            if (map != null)
                return map;

            try
            {
                var mapAPILink = "https://awbw.amarriner.com/matsuzen/api/map/map_info.php?maps_id=" + mapID;

                using (var jsonRequest = new GenericJsonWebRequest(mapAPILink))
                {
                    await jsonRequest.PerformAsync().ConfigureAwait(false);

                    if (jsonRequest.ResponseObject == null)
                        throw new Exception("Failed to download map."); //Todo: Change exception type.

                    //Double check if we already got the map by the time this got through.
                    //Todo: Maybe keep track of the requests we already have.
                    map = Get(mapID);
                    if (map != null)
                        return map;

                    return ParseAndStoreResponseJson(mapID, jsonRequest.ResponseObject);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Failed to download map: " + e.Message);
            }

            var mapPagelink = "https://awbw.amarriner.com/text_map.php?maps_id=" + mapID;

            using (var webRequest = new WebRequest(mapPagelink))
            {
                await webRequest.PerformAsync().ConfigureAwait(false);

                if (webRequest.ResponseStream.Length <= 100)
                    throw new Exception($"Unable to find the map with ID '{mapID}'. Is the session cookie correct?");

                //Double check if we already got the map by the time this got through.
                //Todo: Maybe keep track of the requests we already have.
                map = Get(mapID);
                if (map != null)
                    return map;

                return ParseAndStoreResponseHTML(mapID, webRequest.GetResponseString());
            }
        }

        public IEnumerable<string> GetAvailableResources() => underlyingStorage.GetFiles("");

        public Stream GetStream(string name) => underlyingStorage.GetStream($"{name}.json");

        public ReplayMap ParseAndStoreResponseJson(long gameId, JObject json)
        {
            var terrainFile = new ReplayMap
            {
                TerrainName = (string)json["Name"],
                Size = new Vector2I((int)json["Size X"], (int)json["Size Y"])
            };
            terrainFile.Ids = new short[terrainFile.Size.X * terrainFile.Size.Y];

            var mapArray = (JArray)json["Terrain Map"];

            for (int x = 0; x < terrainFile.Size.X; x++)
            {
                var column = mapArray[x];
                for (int y = 0; y < terrainFile.Size.Y; y++)
                    terrainFile.Ids[y * terrainFile.Size.X + x] = (short)column[y];
            }

            var terrainFileSerialised = JsonConvert.SerializeObject(terrainFile);

            using (var stream = underlyingStorage.GetStream($"{gameId}.json", FileAccess.Write, FileMode.Create))
            {
                using (var sw = new StreamWriter(stream))
                    sw.Write(terrainFileSerialised);
            }

            return terrainFile;
        }

        public ReplayMap ParseAndStoreResponseHTML(long gameId, string html)
        {
            var mapTitleSearch = "<tr><td class=\"bordertitle\"><a class=\"bordertitle\" href=\"prevmaps.php?maps_id=" + gameId + "\">";

            var mapTitleIndex = html.IndexOf(mapTitleSearch, StringComparison.InvariantCulture);
            if (mapTitleIndex < 0)
                throw new Exception("Unexpected map data.");

            mapTitleIndex += mapTitleSearch.Length;

            var htmlShortened = html[mapTitleIndex..];
            var mapTitleEnd = htmlShortened.IndexOf("</a>", StringComparison.InvariantCulture);
            var mapTitle = htmlShortened[..mapTitleEnd];

            var tableEnd = htmlShortened.IndexOf("</table>", StringComparison.InvariantCulture);
            htmlShortened = htmlShortened[mapTitleEnd..tableEnd];

            var values = new List<List<short>>();

            do
            {
                var rowStartIndex = htmlShortened.IndexOf("<td>", StringComparison.InvariantCulture);
                if (rowStartIndex < 0)
                    break;

                htmlShortened = htmlShortened[(rowStartIndex + 4)..];

                var row = new List<short>();
                var idx = 0;
                var numStart = 0;

                while (true)
                {
                    var character = htmlShortened[idx++];

                    if (character == '<')
                    {
                        row.Add(numStart == idx - 1 ? (short)0 : short.Parse(htmlShortened.Substring(numStart, idx - numStart - 1)));
                        break;
                    }

                    if (character == ',')
                    {
                        row.Add(numStart == idx - 1 ? (short)0 : short.Parse(htmlShortened.Substring(numStart, idx - numStart - 1)));
                        numStart = idx;
                    }
                }
                values.Add(row);
            } while (true);

            var terrainFile = new ReplayMap
            {
                TerrainName = mapTitle,
                Size = new Vector2I(values[0].Count, values.Count)
            };
            terrainFile.Ids = new short[terrainFile.Size.X * terrainFile.Size.Y];

            var terrainIdx = 0;

            foreach (var row in values)
            {
                foreach (var tile in row)
                    terrainFile.Ids[terrainIdx++] = tile;
            }

            var terrainFileSerialised = JsonConvert.SerializeObject(terrainFile);

            using (var stream = underlyingStorage.GetStream($"{gameId}.json", FileAccess.Write, FileMode.Create))
            {
                using (var sw = new StreamWriter(stream))
                    sw.Write(terrainFileSerialised);
            }

            return terrainFile;
        }

        public Task<ReplayMap> GetAsync(string name, CancellationToken token) => throw new NotSupportedException();

        #region Disposable

        private bool isDisposed;

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        ~MapFileStorage()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        #endregion
    }
}
