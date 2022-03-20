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
}
