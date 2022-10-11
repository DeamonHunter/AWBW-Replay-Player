using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace AWBWApp.Game.IO
{
    public class MapFileStorage : IResourceStore<ReplayMap>
    {
        private const string terrain_folder = "ReplayData/Terrain";

        private readonly Storage underlyingStorage;

        private readonly Dictionary<long, (string, Texture)> mapTextures = new Dictionary<long, (string, Texture)>();

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

        private Queue<(long, TaskCompletionSource<ReplayMap>)> mapsToDownload = new Queue<(long, TaskCompletionSource<ReplayMap>)>();

        public bool HasMap(long mapID) => underlyingStorage.Exists($"{mapID}.json");

        public async Task<ReplayMap> GetOrAwaitDownloadMap(long mapID)
        {
            var map = Get(mapID);
            if (map != null)
                return map;

            TaskCompletionSource<ReplayMap> task;

            lock (mapsToDownload)
            {
                var contains = mapsToDownload.FirstOrDefault(x => x.Item1 == mapID);

                if (contains.Item2 == null)
                {
                    task = new TaskCompletionSource<ReplayMap>();
                    mapsToDownload.Enqueue((mapID, task));
                }
                else
                    task = contains.Item2;
            }

            return await task.Task.ConfigureAwait(false);
        }

        private DateTime lastDownloaded;
        private bool downloadingMap;

        public void CheckForMapsToDownload()
        {
            if (downloadingMap || (DateTime.UtcNow - lastDownloaded).TotalSeconds < 1)
                return;

            lock (mapsToDownload)
            {
                if (mapsToDownload.Count <= 0)
                    return;

                var next = mapsToDownload.Dequeue();
                Task.Run(() => downloadMap(next.Item1, next.Item2));
            }
        }

        private async void downloadMap(long mapID, TaskCompletionSource<ReplayMap> completionSource)
        {
            var map = Get(mapID);

            if (map != null)
            {
                completionSource.SetResult(map);
                return;
            }

            downloadingMap = true;
            var errorCount = 0;

            while (true)
            {
                try
                {
                    var mapAPILink = "https://awbw.amarriner.com/matsuzen/api/map/map_info.php?maps_id=" + mapID;

                    using (var jsonRequest = new GenericJsonWebRequest(mapAPILink))
                    {
                        await jsonRequest.PerformAsync().ConfigureAwait(false);

                        if (jsonRequest.ResponseObject == null)
                        {
                            errorCount++;

                            if (errorCount > 3)
                            {
                                completionSource.SetException(new Exception($"Failed to download map '{mapID}"));
                                downloadingMap = false;
                                lastDownloaded = DateTime.UtcNow;
                                return;
                            }

                            await Task.Delay(1000);
                            continue;
                        }

                        map = ParseAndStoreResponseJson(mapID, jsonRequest.ResponseObject);
                        completionSource.SetResult(map);

                        downloadingMap = false;
                        lastDownloaded = DateTime.UtcNow;
                        return;
                    }
                }
                catch
                {
                    errorCount++;

                    if (errorCount > 3)
                    {
                        completionSource.SetException(new Exception($"Failed to download map '{mapID}"));
                        downloadingMap = false;
                        lastDownloaded = DateTime.UtcNow;
                        return;
                    }

                    await Task.Delay(1000);
                }
            }
        }

        public async Task<(string, Texture)> GetTextureForMap(long mapID, IRenderer renderer, TerrainTileStorage tileStorage, BuildingStorage buildingStorage, CountryStorage countryStorage)
        {
            if (mapTextures.TryGetValue(mapID, out var existingTexture))
                return existingTexture;

            var map = await GetOrAwaitDownloadMap(mapID);

            Texture texture = null;
            if (map != null)
                texture = map.GenerateTexture(renderer, tileStorage, buildingStorage, countryStorage);

            var tuple = (map?.TerrainName ?? $"Missing Map: {mapID}", texture);

            mapTextures[mapID] = tuple;
            return tuple;
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
                {
                    var value = column[y];
                    if (value == null || value.Type == JTokenType.Null || value.Type == JTokenType.String)
                        terrainFile.Ids[y * terrainFile.Size.X + x] = 0;
                    else
                        terrainFile.Ids[y * terrainFile.Size.X + x] = (short)value;
                }
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
