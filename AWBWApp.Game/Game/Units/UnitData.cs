using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Units
{
    public class UnitData
    {
        public string Name { get; set; }
        public int AWBWId { get; set; }
        public int MaxFuel { get; set; }
        public int MaxAmmo { get; set; }
        public int MovementRange { get; set; }
        public int FuelUsagePerTurn { get; set; }
        public int Vision { get; set; }
        public Vector2I AttackRange { get; set; }
        public int Cost { get; set; }
        public MovementType MovementType { get; set; }
        public Dictionary<string, string> BaseTextureByTeam { get; set; }
        public double[] Frames { get; set; }
        public double FrameOffset { get; set; }
        public bool SecondWeapon { get; set; }
    }
}
