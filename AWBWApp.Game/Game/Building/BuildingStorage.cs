using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Building
{
    public class BuildingStorage
    {
        readonly Dictionary<int, BuildingTile> buildingsByAWBWId = new Dictionary<int, BuildingTile>();
        readonly Dictionary<string, BuildingTile> buildingsByCode = new Dictionary<string, BuildingTile>();

        public void LoadStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                JsonConvert.PopulateObject(reader.ReadToEnd(), buildingsByCode);
            }

            foreach (var tile in buildingsByCode)
                buildingsByAWBWId.Add(tile.Value.AWBWId, tile.Value);
        }

        public BuildingTile GetBuildingByAWBWId(int id)
        {
            return buildingsByAWBWId[id];
        }

        public bool TryGetBuildingByAWBWId(int id, out BuildingTile building)
        {
            return buildingsByAWBWId.TryGetValue(id, out building);
        }

        public bool ContainsBuildingWithAWBWId(int id) => buildingsByAWBWId.ContainsKey(id);
    }
}
