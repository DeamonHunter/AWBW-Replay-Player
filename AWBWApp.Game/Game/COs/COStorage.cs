﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.COs
{
    public class COStorage
    {
        private readonly Dictionary<int, COData> coByAWBWId = new Dictionary<int, COData>();
        private readonly Dictionary<string, COData> coByName = new Dictionary<string, COData>();

        public void LoadStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                JsonConvert.PopulateObject(reader.ReadToEnd(), coByName);
            }

            foreach (var co in coByName)
                coByAWBWId.Add(co.Value.AWBWID, co.Value);
        }

        public COData GetCOByAWBWId(int id) => coByAWBWId[id];
        public bool TryGetCOByAWBWId(int id, out COData co) => coByAWBWId.TryGetValue(id, out co);

        public COData GetCOByName(string name) => coByName[name];

        public List<int> GetAllCOIDs()
        {
            return coByAWBWId.Keys.ToList();
        }
    }
}
