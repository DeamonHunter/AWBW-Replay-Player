using System.Collections.Generic;
using Newtonsoft.Json;

namespace AWBWApp.Game.Game.Country
{
    public class CountryData
    {
        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string Code { get; set; }

        [JsonProperty]
        public int AWBWID { get; set; }

        [JsonProperty]
        public string UnitPath { get; set; }

        [JsonProperty]
        public FaceDirection FaceDirection { get; set; }

        [JsonProperty]
        public Dictionary<string, string> Colours { get; set; } //Todo: Work out how to use direct colours in JSON

        public override string ToString()
        {
            return Name;
        }
    }

    public enum FaceDirection
    {
        Left,
        Right
    }
}
