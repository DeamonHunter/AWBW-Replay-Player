using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Building
{
    public class BuildingStorage
    {
        private readonly Dictionary<int, BuildingTile> buildingsByAWBWId = new Dictionary<int, BuildingTile>();
        private readonly Dictionary<string, BuildingTile> buildingsByCode = new Dictionary<string, BuildingTile>();
        private readonly Dictionary<string, Dictionary<int, BuildingTile>> buildingsByTypeThenCountry = new Dictionary<string, Dictionary<int, BuildingTile>>();

        public void LoadStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                JsonConvert.PopulateObject(reader.ReadToEnd(), buildingsByCode);
            }

            foreach (var tile in buildingsByCode)
            {
                buildingsByAWBWId.Add(tile.Value.AWBWID, tile.Value);

                if (tile.Value.CountryID != -1)
                {
                    if (tile.Value.BuildingType == null)
                        throw new Exception($"Building with code `{tile.Key} has a country, but not a type");

                    if (!buildingsByTypeThenCountry.TryGetValue(tile.Value.BuildingType, out var buildingsByCountry))
                    {
                        buildingsByCountry = new Dictionary<int, BuildingTile>();
                        buildingsByTypeThenCountry[tile.Value.BuildingType] = buildingsByCountry;
                    }

                    buildingsByCountry.Add(tile.Value.CountryID, tile.Value);
                }
            }
        }

        public BuildingTile GetBuildingByAWBWId(int id) => buildingsByAWBWId[id];

        public bool TryGetBuildingByAWBWId(int id, out BuildingTile building) => buildingsByAWBWId.TryGetValue(id, out building);

        public BuildingTile GetBuildingByTypeAndCountry(string type, int countryID) => buildingsByTypeThenCountry[type][countryID];

        public bool ContainsBuildingWithAWBWId(int id) => buildingsByAWBWId.ContainsKey(id);
    }
}
