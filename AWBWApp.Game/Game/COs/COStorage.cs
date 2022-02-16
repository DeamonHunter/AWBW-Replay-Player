using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.COs
{
    public class COStorage
    {
        readonly Dictionary<int, COData> coByAWBWId = new Dictionary<int, COData>();
        readonly Dictionary<string, COData> coByName = new Dictionary<string, COData>();

        public void LoadStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                JsonConvert.PopulateObject(reader.ReadToEnd(), coByName);
            }

            foreach (var co in coByName)
                coByAWBWId.Add(co.Value.AWBWId, co.Value);
        }

        public COData GetCOByAWBWId(int id)
        {
            return coByAWBWId[id];
        }

        public COData GetCOByName(string name)
        {
            return coByName[name];
        }
    }
}
