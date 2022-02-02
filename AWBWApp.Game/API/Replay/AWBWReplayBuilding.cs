using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayBuilding
    {
        public int ID;
        public int? TerrainID;
        public Vector2I Position;
        public int Capture;
        public int LastCapture;
        public string Team;
    }
}
