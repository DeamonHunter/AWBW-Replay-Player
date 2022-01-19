using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWBWApp.Game.API.Replay;
using Newtonsoft.Json;
using osu.Framework.Graphics.Primitives;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;

namespace AWBWApp.Game.IO
{
    public class MapFileStorage : IResourceStore<ReplayMap>
    {
        private const string terrain_folder = "ReplayData/Terrain";

        public MapFileStorage()
        {
            //Ensure that the replay directory always exists before getting it.
            if (!Directory.Exists(terrain_folder))
                Directory.CreateDirectory(terrain_folder);
            Logger.Log("Checked for directory.");
        }

        public ReplayMap Get(string name)
        {
            using (var stream = GetStream(name))
            {
                if (stream == null)
                    return null;

                using (StreamReader sr = new StreamReader(stream))
                    return JsonConvert.DeserializeObject<ReplayMap>(sr.ReadToEnd());
            }
        }

        public ReplayMap Get(int terrainId) => Get(terrainId.ToString());

        public Task<ReplayMap> GetAsync(string name)
        {
            throw new System.NotImplementedException(); //Todo: Is there gonna be a case where we don't check this?
        }

        public IEnumerable<string> GetAvailableResources() => Directory.GetFiles(terrain_folder);

        public Stream GetStream(string name)
        {
            var path = $"{terrain_folder}/{name}.json";
            if (!File.Exists(path))
                return null;
            return File.OpenRead(path);
        }

        public ReplayMap ParseAndStoreResponseHTML(long gameId, string html)
        {
            var mapTitleSearch = "<tr><td class=\"bordertitle\"><a class=\"bordertitle\" href=\"prevmaps.php?maps_id=" + gameId + "\">";

            var mapTitleIndex = html.IndexOf(mapTitleSearch);
            if (mapTitleIndex < 0)
                throw new Exception("Unexpected map data.");
            mapTitleIndex += mapTitleSearch.Length;

            var htmlShortened = html.Substring(mapTitleIndex);
            var mapTitleEnd = htmlShortened.IndexOf("</a>");
            var mapTitle = htmlShortened.Substring(0, mapTitleEnd);

            var tableEnd = htmlShortened.IndexOf("</table>");
            htmlShortened = htmlShortened.Substring(mapTitleEnd, tableEnd - mapTitleEnd);

            var values = new List<List<short>>();

            do
            {
                var rowStartIndex = htmlShortened.IndexOf("<td>");
                if (rowStartIndex < 0)
                    break;

                htmlShortened = htmlShortened.Substring(rowStartIndex + 4);

                var row = new List<short>();
                var idx = 0;
                var numStart = 0;

                while (true)
                {
                    var character = htmlShortened[idx++];

                    if (character == '<')
                    {
                        if (numStart != idx - 1)
                            row.Add(short.Parse(htmlShortened.Substring(numStart, idx - numStart - 1)));
                        break;
                    }

                    if (character == ',')
                    {
                        row.Add(short.Parse(htmlShortened.Substring(numStart, idx - numStart - 1)));
                        numStart = idx;
                    }
                }
                values.Add(row);
            } while (true);

            var terrainFile = new ReplayMap();
            terrainFile.TerrainName = mapTitle;
            terrainFile.Size = new Vector2I(values[0].Count, values.Count);
            terrainFile.Ids = new short[terrainFile.Size.X * terrainFile.Size.Y];

            var terrainIdx = 0;

            foreach (var row in values)
            {
                foreach (var tile in row)
                    terrainFile.Ids[terrainIdx++] = tile;
            }

            var terrainFileSerialised = JsonConvert.SerializeObject(terrainFile);

            //Todo: Was does non-attached debug need this
            if (!Directory.Exists(terrain_folder))
                Directory.CreateDirectory(terrain_folder);
            var path = $"{terrain_folder}/{gameId}.json";
            File.WriteAllText(path, terrainFileSerialised);

            return terrainFile;
        }

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
