using System;

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

        public ReplayMissingBuildingException(long buildingID)
            : base($"Building ID '{buildingID}' was missing")
        {
            BuildingID = buildingID;
        }
    }
}
