using System;
using System.Collections.Generic;
using System.IO;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Tile
{
    public class TerrainTileStorage
    {
        readonly Dictionary<int, TerrainTile> tilesByAWBWId = new Dictionary<int, TerrainTile>();
        readonly Dictionary<string, TerrainTile> tilesByCode = new Dictionary<string, TerrainTile>();

        public void LoadStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                JsonConvert.PopulateObject(reader.ReadToEnd(), tilesByCode);
            }

            foreach (var tile in tilesByCode)
                tilesByAWBWId.Add(tile.Value.AWBWID, tile.Value);
        }

        public TerrainTile GetTileByCode(string code)
        {
            return tilesByCode[code];
        }

        public TerrainTile GetTileByAWBWId(int id)
        {
            return tilesByAWBWId[id];
        }

        public bool TryGetTileByAWBWId(int id, out TerrainTile tile)
        {
            return tilesByAWBWId.TryGetValue(id, out tile);
        }

        public TerrainTile GetRandomTerrainTile(Random random)
        {
            return random.Pick(tilesByAWBWId);
        }
    }
}
