﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Country
{
    public class CountryStorage
    {
        public int HighestCountryId { get; private set; }

        private readonly Dictionary<int, CountryData> countriesByAWBWID = new Dictionary<int, CountryData>();
        private readonly Dictionary<string, CountryData> countriesByCode = new Dictionary<string, CountryData>();
        private readonly Dictionary<string, CountryData> countriesByName = new Dictionary<string, CountryData>();

        public void LoadStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                JsonConvert.PopulateObject(reader.ReadToEnd(), countriesByName);
            }

            foreach (var co in countriesByName)
            {
                HighestCountryId = HighestCountryId < co.Value.AWBWID ? co.Value.AWBWID : HighestCountryId;
                countriesByAWBWID.Add(co.Value.AWBWID, co.Value);
                countriesByCode.Add(co.Value.Code, co.Value);
            }
        }

        public CountryData SafeGetCountryByAWBWID(int id)
        {
            return countriesByAWBWID.TryGetValue(id, out var country) ? country : countriesByAWBWID[HighestCountryId];
        }

        public CountryData GetCountryByCode(string name)
        {
            return countriesByCode[name];
        }

        public List<int> GetAllCountryIDs()
        {
            return countriesByAWBWID.Keys.ToList();
        }
    }
}
