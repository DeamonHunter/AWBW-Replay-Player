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
        public bool SecondWeapon { get; set; }
        public Animation IdleAnimation;
        public Animation MoveSideAnimation;
        public Animation MoveUpAnimation;
        public Animation MoveDownAnimation;
    }

    public class Animation
    {
        public string Texture;
        public double[] Frames;
        public double FrameOffset;
    }
}
