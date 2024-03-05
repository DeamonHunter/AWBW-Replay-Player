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
            //For destroyed Pipeseams Capture is undefined
            if (TerrainID != 115 && TerrainID != 116 && Capture != other.Capture)
                return false;

            return true;
        }
    }
}
