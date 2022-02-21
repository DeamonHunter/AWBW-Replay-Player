using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Country
{
    public class CountryStorage
    {
        readonly Dictionary<int, CountryData> countriesByAWBWID = new Dictionary<int, CountryData>();
        readonly Dictionary<string, CountryData> countriesByCode = new Dictionary<string, CountryData>();
        readonly Dictionary<string, CountryData> countriesByName = new Dictionary<string, CountryData>();

        public void LoadStream(Stream jsonStream)
        {
            using (var reader = new StreamReader(jsonStream))
            {
                JsonConvert.PopulateObject(reader.ReadToEnd(), countriesByName);
            }

            foreach (var co in countriesByName)
            {
                countriesByAWBWID.Add(co.Value.AWBWID, co.Value);
                countriesByCode.Add(co.Value.Code, co.Value);
            }
        }

        public CountryData GetCountryByAWBWID(int id)
        {
            return countriesByAWBWID[id];
        }

        public CountryData GetCountryByCode(string name)
        {
            return countriesByCode[name];
        }
    }
}
