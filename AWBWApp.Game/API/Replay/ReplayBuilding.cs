using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayBuilding
    {
        public long ID;
        public int? TerrainID;
        public Vector2I Position;
        public int Capture;
        public int LastCapture;
        public string Team;

        public void Copy(ReplayBuilding other)
        {
            ID = other.ID;
            TerrainID = other.TerrainID;
            Position = other.Position;
            Capture = other.Capture;
            LastCapture = other.LastCapture;
            Team = other.Team;
        }

        public ReplayBuilding Clone()
        {
            var building = new ReplayBuilding();
            building.Copy(this);
            return building;
        }
    }
}
