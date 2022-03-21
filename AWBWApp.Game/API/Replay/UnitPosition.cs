using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public class UnitPosition
    {
        public bool UnitVisible { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public UnitPosition() { }

        public UnitPosition(Vector2I position)
        {
            X = position.X;
            Y = position.Y;
            UnitVisible = true;
        }
    }
}
