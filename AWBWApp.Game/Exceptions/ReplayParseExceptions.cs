using System;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Exceptions
{
    public class ReplayMissingUnitException : Exception
    {
        public long UnitId { get; }

        public ReplayMissingUnitException(long unitId)
            : base($"Unit ID '{unitId}' was missing")
        {
            UnitId = unitId;
        }
    }

    public class ReplayMissingBuildingException : Exception
    {
        public long BuildingID { get; }
        public Vector2I BuildingPosition { get; }

        public ReplayMissingBuildingException(long buildingID)
            : base($"Building ID '{buildingID}' was missing")
        {
            BuildingID = buildingID;
        }

        public ReplayMissingBuildingException(Vector2I buildingPosition)
            : base($"Building at Position '{{{buildingPosition.X},{buildingPosition.Y}}}' was missing")
        {
            BuildingPosition = buildingPosition;
        }
    }

    public class CorruptedReplayException : Exception
    {
        public CorruptedReplayException()
            : base($"Cannot parse replay. The replay is likely corrupted and cannot be opened.")
        {
        }
    }
}
