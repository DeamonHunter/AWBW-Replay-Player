using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayBuilding
    {
        public long ID;
        public int? TerrainID;
        public Vector2I Position;
        public int? Capture;
        public int? LastCapture;
        public string Team;

        private const int h_pipe_rubble = 115;
        private const int v_pipe_rubble = 116;

        public void Overwrite(ReplayBuilding other)
        {
            ID = other.ID;
            Position = other.Position;

            TerrainID = other.TerrainID ?? TerrainID;
            Capture = other.Capture ?? Capture;
            LastCapture = other.LastCapture ?? LastCapture;
            Team = other.Team ?? Team;
        }

        public ReplayBuilding Clone()
        {
            var building = new ReplayBuilding();
            building.Overwrite(this);
            return building;
        }

        public bool DoesBuildingMatch(ReplayBuilding other)
        {
            if (ID != other.ID)
                return false;
            if (TerrainID != other.TerrainID)
                return false;
            if (Position != other.Position)
                return false;

            //Destroyed pipes have an undefined capture which can cause issues so skip over that.
            if (TerrainID != h_pipe_rubble && TerrainID != v_pipe_rubble && Capture != other.Capture)
                return false;

            return true;
        }
    }
}
