using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Units
{
    public class UnitStorage
    {
        readonly Dictionary<int, UnitData> unitsByAWBWId = new Dictionary<int, UnitData>();
        readonly Dictionary<string, UnitData> unitsByCode = new Dictionary<string, UnitData>();

        public void LoadStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                JsonConvert.PopulateObject(reader.ReadToEnd(), unitsByCode);
            }

            foreach (var tile in unitsByCode)
                unitsByAWBWId.Add(tile.Value.AWBWId, tile.Value);
        }

        public UnitData GetUnitByAWBWId(int id)
        {
            return unitsByAWBWId[id];
        }

        public UnitData GetUnitByCode(string code)
        {
            return unitsByCode[code];
        }
    }
}
