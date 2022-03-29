using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;

namespace AWBWApp.Game.Game.Building
{
    public class BuildingTile
    {
        public int AWBWID { get; set; }

        public int CountryID { get; set; }

        public int BaseDefence { get; set; }

        public int SightDistanceIncrease { get; set; }

        public bool GivesMoneyWhenCaptured { get; set; } = true;

        public int LimitFogOfWarSightDistance { get; set; } = -1;

        public Dictionary<MovementType, int> MovementCostsPerType { get; set; }

        public string Colour { get; set; }

        public Dictionary<Weather, string> Textures { get; set; }

        public double[] Frames { get; set; }

        public double FrameOffset { get; set; }

        public string Name { get; set; }

        public string BuildingType { get; set; }
    }
}
